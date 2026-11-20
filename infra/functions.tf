# The sample's function app (docs/DECISIONS.md D1–D7): a function app on Container Apps (azd service receipt-functions),
# the storage account of its Functions host, and the data account where receipts are uploaded. Event Grid turns each
# upload to the receipts container into a message on the receipts-uploaded queue, which triggers the functions; the
# summaries go to the ReceiptSummaries table. Everything is reached with managed identities: no keys (D3, D4).

variable "receipt_functions_image" {
  description = "Image of the receipt-functions service, set by azd deploy (SERVICE_RECEIPT_FUNCTIONS_IMAGE_NAME). Empty: not deployed yet."
  type        = string
  default     = ""
}

locals {
  receipts_container = "receipts"
  receipts_queue     = "receipts-uploaded"
  summaries_table    = "ReceiptSummaries"
}

module "host_storage" {
  source              = "./modules/storage-account"
  name                = "sthost${local.compact_name}${local.token}"
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  tags                = local.tags
  private_endpoint    = local.private_endpoint
  # The Functions host and the functions use all three services.
  private_endpoint_services = ["blob", "queue", "table"]
}

module "data_storage" {
  source              = "./modules/storage-account"
  name                = "stdata${local.compact_name}${local.token}"
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  tags                = local.tags
  private_endpoint    = local.private_endpoint
  # The Functions host and the functions use all three services.
  private_endpoint_services = ["blob", "queue", "table"]
}

# Created through Azure Resource Manager (storage_account_id): the accounts accept no shared keys.
resource "azurerm_storage_container" "receipts" {
  name               = local.receipts_container
  storage_account_id = module.data_storage.id
}

resource "azurerm_storage_queue" "receipts_uploaded" {
  name               = local.receipts_queue
  storage_account_id = module.data_storage.id
}

resource "azurerm_storage_table" "receipt_summaries" {
  name               = local.summaries_table
  storage_account_id = module.data_storage.id
}

# Event Grid delivers the data account's BlobCreated events to the queue with the topic's own identity (D3).
resource "azurerm_eventgrid_system_topic" "data_storage" {
  name                = "evgt-${var.environment_name}-${local.token}"
  resource_group_name = azurerm_resource_group.main.name
  location            = var.location
  source_resource_id  = module.data_storage.id
  topic_type          = "Microsoft.Storage.StorageAccounts"
  tags                = local.tags

  identity {
    type = "SystemAssigned"
  }
}

resource "azurerm_role_assignment" "event_grid_queue_sender" {
  scope                = module.data_storage.id
  role_definition_name = "Storage Queue Data Message Sender"
  principal_id         = azurerm_eventgrid_system_topic.data_storage.identity[0].principal_id
  principal_type       = "ServicePrincipal"
}

resource "azurerm_eventgrid_system_topic_event_subscription" "receipts_uploaded" {
  name                 = "receipts-uploaded"
  system_topic         = azurerm_eventgrid_system_topic.data_storage.name
  resource_group_name  = azurerm_resource_group.main.name
  included_event_types = ["Microsoft.Storage.BlobCreated"]

  subject_filter {
    subject_begins_with = "/blobServices/default/containers/${local.receipts_container}/"
  }

  storage_queue_endpoint {
    storage_account_id = module.data_storage.id
    queue_name         = azurerm_storage_queue.receipts_uploaded.name
  }

  delivery_identity {
    type = "SystemAssigned"
  }

  # Event Grid checks the endpoint when the subscription is created: the topic must already be allowed to send.
  depends_on = [azurerm_role_assignment.event_grid_queue_sender]
}

module "receipt_functions" {
  source                                    = "./modules/function-app-container-apps"
  name                                      = "func-${local.compact_name}-${local.token}"
  resource_group_name                       = azurerm_resource_group.main.name
  resource_group_id                         = azurerm_resource_group.main.id
  location                                  = var.location
  tags                                      = local.tags
  azd_service_name                          = "receipt-functions"
  image                                     = var.receipt_functions_image
  container_apps_environment_id             = module.container_apps_environment.id
  container_apps_environment_default_domain = module.container_apps_environment.default_domain
  registry_login_server                     = module.container_registry.login_server
  registry_pull_identity_id                 = module.container_registry.pull_identity_id
  host_storage_account_id                   = module.host_storage.id
  host_storage_account_name                 = module.host_storage.name
  application_insights_connection_string    = module.monitoring.application_insights_connection_string

  # DataStorage__credential and DataStorage__clientId are set by the module, with the app's identity.
  identity_connection_prefixes = ["DataStorage"]
  env = {
    DataStorage__blobServiceUri = module.data_storage.primary_blob_endpoint
    # The storage-account module has no queue endpoint output; it differs from the blob one only by the service name.
    DataStorage__queueServiceUri = replace(module.data_storage.primary_blob_endpoint, ".blob.", ".queue.")
    DataStorage__tableServiceUri = module.data_storage.primary_table_endpoint
  }

  role_assignments = {
    data_blob = {
      scope                = module.data_storage.id
      role_definition_name = "Storage Blob Data Reader"
    }
    data_queue = {
      scope                = module.data_storage.id
      role_definition_name = "Storage Queue Data Contributor"
    }
    data_table = {
      scope                = module.data_storage.id
      role_definition_name = "Storage Table Data Contributor"
    }
    telemetry = {
      scope                = module.monitoring.application_insights_id
      role_definition_name = "Monitoring Metrics Publisher"
    }
  }
}

output "RECEIPT_FUNCTIONS_URL" { value = module.receipt_functions.url }
output "DATA_STORAGE_ACCOUNT" { value = module.data_storage.name }
output "RECEIPTS_CONTAINER" { value = azurerm_storage_container.receipts.name }
