# A function app on Container Apps: kind functionapp, scaling from 0, the placeholder image on port 80 until azd deploys
# the service's image on 8080, and the host storage reached with the app's identity, never with a connection string.

mock_provider "azurerm" {
  mock_resource "azurerm_user_assigned_identity" {
    override_during = plan
    defaults = {
      id           = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-test/providers/Microsoft.ManagedIdentity/userAssignedIdentities/id-func-test"
      client_id    = "00000000-0000-0000-0000-000000000002"
      principal_id = "00000000-0000-0000-0000-000000000003"
    }
  }
}

mock_provider "azapi" {}

variables {
  name                                      = "func-test"
  resource_group_name                       = "rg-test"
  resource_group_id                         = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-test"
  location                                  = "francecentral"
  azd_service_name                          = "receipt-functions"
  container_apps_environment_id             = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-test/providers/Microsoft.App/managedEnvironments/cae-test"
  container_apps_environment_default_domain = "test.francecentral.azurecontainerapps.io"
  registry_login_server                     = "crtest.azurecr.io"
  registry_pull_identity_id                 = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-test/providers/Microsoft.ManagedIdentity/userAssignedIdentities/id-crtest-pull"
  host_storage_account_id                   = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-test/providers/Microsoft.Storage/storageAccounts/sthosttest"
  host_storage_account_name                 = "sthosttest"
}

run "placeholder_before_deployment" {
  command = plan

  assert {
    condition     = azapi_resource.function_app.body.kind == "functionapp"
    error_message = "The container app is a function app (kind functionapp)."
  }
  assert {
    condition     = azapi_resource.function_app.body.properties.configuration.ingress.targetPort == 80
    error_message = "Without an image, the placeholder listens on port 80."
  }
  assert {
    condition     = azapi_resource.function_app.body.properties.template.containers[0].image == "mcr.microsoft.com/azure-functions/dotnet8-quickstart-demo:1.0"
    error_message = "Without an image, the placeholder image runs."
  }
  assert {
    condition     = azapi_resource.function_app.body.properties.template.scale.minReplicas == 0
    error_message = "The app scales to zero."
  }
  assert {
    condition     = azapi_resource.function_app.body.properties.template.scale.maxReplicas == 10
    error_message = "At most 10 replicas by default."
  }
  assert {
    condition     = azapi_resource.function_app.tags["azd-service-name"] == "receipt-functions"
    error_message = "azd finds the app by its service name tag."
  }
}

run "service_image_after_deployment" {
  command = plan

  variables {
    image = "crtest.azurecr.io/receipt-functions:azd-deploy-1"
  }

  assert {
    condition     = azapi_resource.function_app.body.properties.configuration.ingress.targetPort == 8080
    error_message = "The service's image listens on port 8080."
  }
  assert {
    condition     = azapi_resource.function_app.body.properties.template.containers[0].image == "crtest.azurecr.io/receipt-functions:azd-deploy-1"
    error_message = "The deployed image runs."
  }
}

run "host_storage_with_identity" {
  command = plan

  assert {
    condition     = !contains([for setting in azapi_resource.function_app.body.properties.template.containers[0].env : setting.name], "AzureWebJobsStorage")
    error_message = "No AzureWebJobsStorage connection string."
  }
  assert {
    condition     = local.settings["AzureWebJobsStorage__credential"] == "managedidentity" && local.settings["AzureWebJobsStorage__accountName"] == "sthosttest"
    error_message = "The host reaches its storage account with the app's identity."
  }
  assert {
    condition = toset([for key in ["host_blob", "host_queue", "host_table"] : azurerm_role_assignment.app[key].role_definition_name]) == toset([
      "Storage Blob Data Owner", "Storage Queue Data Contributor", "Storage Table Data Contributor",
    ])
    error_message = "The app's identity has the host storage roles."
  }
  assert {
    condition     = alltrue([for key in ["host_blob", "host_queue", "host_table"] : azurerm_role_assignment.app[key].scope == var.host_storage_account_id])
    error_message = "The host storage roles are on the host storage account."
  }
  assert {
    condition     = !contains(keys(local.settings), "APPLICATIONINSIGHTS_CONNECTION_STRING")
    error_message = "No telemetry settings without a connection string."
  }
}

run "identity_connections_telemetry_and_roles" {
  command = plan

  variables {
    application_insights_connection_string = "InstrumentationKey=00000000-0000-0000-0000-000000000004"
    identity_connection_prefixes           = ["DataStorage"]
    env = {
      DataStorage__blobServiceUri = "https://stdatatest.blob.core.windows.net/"
    }
    role_assignments = {
      data_blob = {
        scope                = "/subscriptions/00000000-0000-0000-0000-000000000000/resourceGroups/rg-test/providers/Microsoft.Storage/storageAccounts/stdatatest"
        role_definition_name = "Storage Blob Data Reader"
      }
    }
  }

  assert {
    condition     = local.settings["DataStorage__credential"] == "managedidentity" && local.settings["DataStorage__clientId"] == "00000000-0000-0000-0000-000000000002"
    error_message = "Each identity connection prefix uses the app's identity."
  }
  assert {
    condition     = local.settings["DataStorage__blobServiceUri"] == "https://stdatatest.blob.core.windows.net/"
    error_message = "The given settings are passed to the app."
  }
  assert {
    condition     = local.settings["APPLICATIONINSIGHTS_AUTHENTICATION_STRING"] == "ClientId=00000000-0000-0000-0000-000000000002;Authorization=AAD"
    error_message = "Telemetry is sent with Entra authentication."
  }
  assert {
    condition     = azurerm_role_assignment.app["data_blob"].role_definition_name == "Storage Blob Data Reader"
    error_message = "The given roles are assigned to the app's identity."
  }
  assert {
    condition     = length(azurerm_role_assignment.app) == 4
    error_message = "The given roles come in addition to the host storage roles."
  }
}
