@description('Location for all resources')
param location string

@description('Tags for all resources')
param tags object

@description('Name of the Azure AI Services resource')
param name string

@description('GPT model deployment name')
param gptModelDeploymentName string

@description('GPT model name')
param gptModelName string

@description('GPT model version')
param gptModelVersion string

// Azure AI Services resource (Foundry style - provides services.ai.azure.com endpoint)
resource aiServices 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: name
  location: location
  tags: tags
  kind: 'AIServices'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: name
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      defaultAction: 'Allow'
    }
  }
}

// GPT-4o Deployment
resource gptDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: aiServices
  name: gptModelDeploymentName
  sku: {
    name: 'Standard'
    capacity: 50 // Tokens per minute (in thousands) - 50K TPM
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
output aiServicesId string = aiServices.id
output aiServicesName string = aiServices.name
// The inference endpoint for Azure AI Services (Foundry style)
output aiServicesEndpoint string = '${aiServices.properties.endpoint}models'
output gptDeploymentName string = gptDeployment.name
