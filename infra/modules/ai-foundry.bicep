@description('Location for all resources')
param location string

@description('Tags for all resources')
param tags object

@description('Name of the AI Hub')
param aiHubName string

@description('Name of the AI Project')
param aiProjectName string

@description('Storage Account resource ID')
param storageAccountId string

@description('Application Insights resource ID')
param applicationInsightsId string

@description('Azure OpenAI resource ID')
param openAiId string

// AI Hub (formerly Azure Machine Learning workspace with AI capabilities)
resource aiHub 'Microsoft.MachineLearningServices/workspaces@2024-10-01' = {
  name: aiHubName
  location: location
  tags: tags
  kind: 'Hub'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    friendlyName: 'DocCoach AI Hub'
    description: 'AI Hub for DocCoach document analysis'
    storageAccount: storageAccountId
    applicationInsights: applicationInsightsId
    publicNetworkAccess: 'Enabled'
  }
}

// AI Project (connected to the Hub)
resource aiProject 'Microsoft.MachineLearningServices/workspaces@2024-10-01' = {
  name: aiProjectName
  location: location
  tags: tags
  kind: 'Project'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    friendlyName: 'DocCoach AI Project'
    description: 'AI Project for document review analysis'
    hubResourceId: aiHub.id
    publicNetworkAccess: 'Enabled'
  }
}

// Connection to Azure OpenAI from AI Hub
resource openAiConnection 'Microsoft.MachineLearningServices/workspaces/connections@2024-10-01' = {
  parent: aiHub
  name: 'aoai-connection'
  properties: {
    category: 'AzureOpenAI'
    target: openAiId
    authType: 'AAD'
    isSharedToAll: true
    metadata: {
      ApiType: 'Azure'
      ResourceId: openAiId
    }
  }
}

// Outputs
output aiHubId string = aiHub.id
output aiHubName string = aiHub.name
output aiProjectId string = aiProject.id
output aiProjectName string = aiProject.name
output aiProjectEndpoint string = aiProject.properties.discoveryUrl
