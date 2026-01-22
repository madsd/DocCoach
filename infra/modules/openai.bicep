@description('Location for all resources')
param location string

@description('Tags for all resources')
param tags object

@description('Name of the Azure OpenAI service')
param openAiName string

@description('GPT model deployment name')
param gptModelDeploymentName string

@description('GPT model name')
param gptModelName string

@description('GPT model version')
param gptModelVersion string

// Azure OpenAI Service
resource openAi 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: openAiName
  location: location
  tags: tags
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: openAiName
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
    }
  }
}

// GPT-4o Deployment
resource gptDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: openAi
  name: gptModelDeploymentName
  sku: {
    name: 'Standard'
    capacity: 10 // Tokens per minute (in thousands)
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: gptModelName
      version: gptModelVersion
    }
    raiPolicyName: 'Microsoft.DefaultV2'
  }
}

// Outputs
output openAiId string = openAi.id
output openAiName string = openAi.name
output openAiEndpoint string = openAi.properties.endpoint
output gptDeploymentName string = gptDeployment.name
