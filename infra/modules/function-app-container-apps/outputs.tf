output "name" {
  value = azapi_resource.function_app.name
}

output "url" {
  value = "https://${azapi_resource.function_app.name}.${var.container_apps_environment_default_domain}"
}

output "identity_principal_id" {
  value = azurerm_user_assigned_identity.app.principal_id
}

output "identity_client_id" {
  value = azurerm_user_assigned_identity.app.client_id
}
