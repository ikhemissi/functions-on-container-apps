variable "name" {
  description = "Container app name: 2 to 32 lowercase letters, digits and hyphens."
  type        = string
}

variable "resource_group_name" {
  type = string
}

variable "resource_group_id" {
  description = "Parent of the container app (azapi)."
  type        = string
}

variable "location" {
  type = string
}

variable "tags" {
  type    = map(string)
  default = {}
}

variable "azd_service_name" {
  description = "The service of azure.yaml deployed to this app (tag azd-service-name)."
  type        = string
}

variable "container_apps_environment_id" {
  type = string
}

variable "container_apps_environment_default_domain" {
  description = "Gives the app's URL before it exists (https://<name>.<default domain>)."
  type        = string
}

variable "registry_login_server" {
  type = string
}

variable "registry_pull_identity_id" {
  description = "User-assigned identity with AcrPull on the registry (the container-registry module's pull_identity_id)."
  type        = string
}

variable "image" {
  description = "Image deployed by azd (SERVICE_<NAME>_IMAGE_NAME). Empty: not deployed yet, the placeholder image runs on port 80."
  type        = string
  default     = ""
}

variable "max_replicas" {
  type    = number
  default = 10
}

variable "host_storage_account_id" {
  description = "Storage account of the Functions host (AzureWebJobsStorage), reached with the app's identity."
  type        = string
}

variable "host_storage_account_name" {
  type = string
}

variable "application_insights_connection_string" {
  description = "Telemetry target, sent with Entra authentication (the app's identity needs Monitoring Metrics Publisher). Null: no telemetry."
  type        = string
  default     = null
  sensitive   = true
}

variable "identity_connection_prefixes" {
  description = "Identity-based connections of the functions: each prefix gets <prefix>__credential = managedidentity and <prefix>__clientId = the app's identity."
  type        = list(string)
  default     = []
}

variable "env" {
  description = "Other settings of the app, by name. No secrets: they are stored in plain text."
  type        = map(string)
  default     = {}
}

variable "role_assignments" {
  description = "Roles of the app's identity, by a static key: the scope and the role name. Assigned before the app starts."
  type = map(object({
    scope                = string
    role_definition_name = string
  }))
  default = {}
}
