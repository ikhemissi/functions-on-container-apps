# An Azure Functions app hosted natively on Container Apps (a container app of kind functionapp, docs/DECISIONS.md D1, D2),
# with its own user-assigned identity: it reaches its host storage, its data and Application Insights with that identity,
# without keys or connection strings (D4). It scales from 0 on the rules Container Apps generates from the triggers (D6).

locals {
  # Until azd deploys the service's image, a public quickstart image that listens on 80; the service's own image
  # listens on 8080 (D5).
  is_image_deployed = var.image != ""
  image             = local.is_image_deployed ? var.image : "mcr.microsoft.com/azure-functions/dotnet8-quickstart-demo:1.0"
  target_port       = local.is_image_deployed ? 8080 : 80

  host_storage_settings = {
    AzureWebJobsStorage__accountName = var.host_storage_account_name
    AzureWebJobsStorage__credential  = "managedidentity"
    AzureWebJobsStorage__clientId    = azurerm_user_assigned_identity.app.client_id
    AZURE_CLIENT_ID                  = azurerm_user_assigned_identity.app.client_id
  }
  identity_connection_settings = merge([
    for prefix in var.identity_connection_prefixes : {
      "${prefix}__credential" = "managedidentity"
      "${prefix}__clientId"   = azurerm_user_assigned_identity.app.client_id
    }
  ]...)
  # Whether it is given is not secret; the connection string itself stays marked sensitive.
  has_telemetry = nonsensitive(var.application_insights_connection_string != null)
  telemetry_settings = local.has_telemetry ? {
    APPLICATIONINSIGHTS_CONNECTION_STRING     = var.application_insights_connection_string
    APPLICATIONINSIGHTS_AUTHENTICATION_STRING = "ClientId=${azurerm_user_assigned_identity.app.client_id};Authorization=AAD"
  } : {}

  settings = merge(var.env, local.identity_connection_settings, local.host_storage_settings, local.telemetry_settings)
  env      = [for name, value in local.settings : { name = name, value = value }]

  # The Functions host needs blobs (leases, keys), queues and tables on its storage account.
  host_storage_roles = {
    host_blob  = "Storage Blob Data Owner"
    host_queue = "Storage Queue Data Contributor"
    host_table = "Storage Table Data Contributor"
  }
  role_assignments = merge(
    { for key, role in local.host_storage_roles : key => { scope = var.host_storage_account_id, role_definition_name = role } },
    var.role_assignments,
  )
}

resource "azurerm_user_assigned_identity" "app" {
  name                = "id-${var.name}"
  resource_group_name = var.resource_group_name
  location            = var.location
  tags                = var.tags
}

resource "azurerm_role_assignment" "app" {
  for_each             = local.role_assignments
  scope                = each.value.scope
  role_definition_name = each.value.role_definition_name
  principal_id         = azurerm_user_assigned_identity.app.principal_id
  principal_type       = "ServicePrincipal"
}

resource "azapi_resource" "function_app" {
  type      = "Microsoft.App/containerApps@2025-07-01"
  name      = var.name
  parent_id = var.resource_group_id
  location  = var.location
  tags      = merge(var.tags, { "azd-service-name" = var.azd_service_name })

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.app.id, var.registry_pull_identity_id]
  }

  body = {
    kind = "functionapp"
    properties = {
      environmentId       = var.container_apps_environment_id
      workloadProfileName = "Consumption"
      configuration = {
        activeRevisionsMode = "Single"
        # Ingress also enables the generated scale rules (D6).
        ingress = {
          external      = true
          targetPort    = local.target_port
          transport     = "Auto"
          allowInsecure = false
        }
        registries = [{
          server   = var.registry_login_server
          identity = var.registry_pull_identity_id
        }]
      }
      template = {
        containers = [{
          name  = "functions"
          image = local.image
          resources = {
            cpu    = 0.5
            memory = "1Gi"
          }
          env = local.env
        }]
        scale = {
          minReplicas = 0
          maxReplicas = var.max_replicas
        }
      }
    }
  }

  # The host fails to start without its storage roles, so they exist before the app does.
  depends_on = [azurerm_role_assignment.app]
}
