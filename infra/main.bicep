targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment (e.g., staging, production)')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string = 'swedencentral'

@description('Name of the resource group')
param resourceGroupName string = ''

@description('Name of the App Service Plan')
param appServicePlanName string = ''

@description('Name of the App Service')
param appServiceName string = ''

@description('Name of the Storage Account')
param storageAccountName string = ''

@description('Name of the Log Analytics Workspace')
param logAnalyticsName string = ''

@description('Name of the Application Insights')
param applicationInsightsName string = ''

@description('Name of the Azure AI Services resource')
param aiServicesName string = ''

@description('GPT model deployment name')
param gptModelDeploymentName string = 'gpt-4o'

@description('GPT model name')
param gptModelName string = 'gpt-4o'

@description('GPT model version')
param gptModelVersion string = '2024-11-20'

// Tags for all resources
var tags = {
  'azd-env-name': environmentName
  'application': 'doccoach'
}

// Generate unique suffix for resource names
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))

// Resource names with defaults
var finalResourceGroupName = !empty(resourceGroupName) ? resourceGroupName : 'rg-${environmentName}'
var finalAppServicePlanName = !empty(appServicePlanName) ? appServicePlanName : 'asp-${resourceToken}'
var finalAppServiceName = !empty(appServiceName) ? appServiceName : 'app-${resourceToken}'
var finalStorageAccountName = !empty(storageAccountName) ? storageAccountName : 'st${resourceToken}'
var finalLogAnalyticsName = !empty(logAnalyticsName) ? logAnalyticsName : 'log-${resourceToken}'
var finalApplicationInsightsName = !empty(applicationInsightsName) ? applicationInsightsName : 'appi-${resourceToken}'
var finalAiServicesName = !empty(aiServicesName) ? aiServicesName : 'ai-${resourceToken}'

// Resource Group
resource rg 'Microsoft.Resources/resourceGroups@2024-03-01' = {
  name: finalResourceGroupName
  location: location
  tags: tags
}

// Monitoring (Log Analytics + Application Insights)
module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring'
  scope: rg
  params: {
    location: location
    tags: tags
    logAnalyticsName: finalLogAnalyticsName
    applicationInsightsName: finalApplicationInsightsName
  }
}

// Storage Account
module storage 'modules/storage-account.bicep' = {
  name: 'storage'
  scope: rg
  params: {
    location: location
    tags: tags
    storageAccountName: finalStorageAccountName
  }
}

// Azure AI Services
module aiServices 'modules/ai-services.bicep' = {
  name: 'aiServices'
  scope: rg
  params: {
    name: finalAiServicesName
    location: location
    tags: tags
    gptModelDeploymentName: gptModelDeploymentName
    gptModelName: gptModelName
    gptModelVersion: gptModelVersion
  }
}

// App Service (Plan + Web App)
module appService 'modules/app-service.bicep' = {
  name: 'appService'
  scope: rg
  params: {
    location: location
    tags: tags
    appServicePlanName: finalAppServicePlanName
    appServiceName: finalAppServiceName
    applicationInsightsConnectionString: monitoring.outputs.applicationInsightsConnectionString
    storageAccountName: storage.outputs.storageAccountName
    aiServicesEndpoint: aiServices.outputs.aiServicesEndpoint
    gptModelDeploymentName: gptModelDeploymentName
  }
}

// RBAC: App Service -> Storage Account (Blob Data Contributor)
module storageBlobRoleAssignment 'modules/role-assignment.bicep' = {
  name: 'storageBlobRoleAssignment'
  scope: rg
  params: {
    principalId: appService.outputs.appServicePrincipalId
    roleDefinitionId: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe' // Storage Blob Data Contributor
    principalType: 'ServicePrincipal'
  }
}

// RBAC: App Service -> Storage Account (Table Data Contributor)
module storageTableRoleAssignment 'modules/role-assignment.bicep' = {
  name: 'storageTableRoleAssignment'
  scope: rg
  params: {
    principalId: appService.outputs.appServicePrincipalId
    roleDefinitionId: '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3' // Storage Table Data Contributor
    principalType: 'ServicePrincipal'
  }
}

// RBAC: App Service -> Azure AI Services (Cognitive Services User - includes MaaS permissions)
module aiServicesRoleAssignment 'modules/role-assignment.bicep' = {
  name: 'aiServicesRoleAssignment'
  scope: rg
  params: {
    principalId: appService.outputs.appServicePrincipalId
    roleDefinitionId: 'a97b65f3-24c7-4388-baec-2e87135dc908' // Cognitive Services User
    principalType: 'ServicePrincipal'
  }
}

// Outputs for azd
output AZURE_LOCATION string = location
output AZURE_RESOURCE_GROUP string = rg.name
output AZURE_APP_SERVICE_NAME string = appService.outputs.appServiceName
output AZURE_APP_SERVICE_URL string = appService.outputs.appServiceUrl
output AZURE_STORAGE_ACCOUNT_NAME string = storage.outputs.storageAccountName
output AZURE_AI_SERVICES_ENDPOINT string = aiServices.outputs.aiServicesEndpoint
output APPLICATIONINSIGHTS_CONNECTION_STRING string = monitoring.outputs.applicationInsightsConnectionString
