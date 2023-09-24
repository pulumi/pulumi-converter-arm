# pulumi-converter-arm

A Pulumi converter plugin to convert ARM templates to Pulumi languages. Currently work in progress.

This plugin uses the converter logic from [pulumi-converter-bicep](https://github.com/Zaid-Ajaj/pulumi-converter-bicep). First it converts the ARM template to Bicep and then uses the Bicep converter to convert to Pulumi languages.

### Installation

```
pulumi plugin install converter arm --server github://api.github.com/Zaid-Ajaj
```

### Usage
In a directory with a single ARM template file, run the following command:
```
pulumi convert --from arm --language <language> --out pulumi -- --entry <entry-file>
```
Will convert ARM template into your language of choice: `typescript`, `csharp`, `python`, `go`, `java` or `yaml`

### Example
```json
{
  "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "storageAccountType": {
      "type": "string",
      "defaultValue": "Standard_LRS",
      "metadata": {
        "description": "Storage Account type"
      }
    },
    "location": {
      "type": "string",
      "defaultValue": "[resourceGroup().location]",
      "metadata": {
        "description": "The storage account location."
      }
    },
    "storageAccountName": {
      "type": "string",
      "defaultValue": "[format('store{0}', uniqueString(resourceGroup().id))]",
      "metadata": {
        "description": "The name of the storage account"
      }
    }
  },
  "resources": [
    {
      "type": "Microsoft.Storage/storageAccounts",
      "apiVersion": "2022-09-01",
      "name": "[parameters('storageAccountName')]",
      "location": "[parameters('location')]",
      "sku": {
        "name": "[parameters('storageAccountType')]"
      },
      "kind": "StorageV2",
      "properties": {}
    }
  ],
  "outputs": {
    "accountName": {
      "type": "string",
      "value": "[parameters('storageAccountName')]"
    },
    "storageAccountId": {
      "type": "string",
      "value": "[resourceId('Microsoft.Storage/storageAccounts', parameters('storageAccountName'))]"
    }
  }
}
```
Converts to `typescript`
```typescript
import * as pulumi from "@pulumi/pulumi";
import * as azure_native from "@pulumi/azure-native";

const config = new pulumi.Config();
// The name of the resource group to operate on
const resourceGroupName = config.require("resourceGroupName");
const currentResourceGroup = azure_native.resources.getResourceGroupOutput({
    resourceGroupName: resourceGroupName,
});
// Storage Account type
const storageAccountType = config.get("storageAccountType") || "Standard_LRS";
// The storage account location.
const location = config.get("location") || currentResourceGroup.apply(currentResourceGroup => currentResourceGroup.location);
// The name of the storage account
const storageAccountName = config.get("storageAccountName") || currentResourceGroup.apply(currentResourceGroup => `store${currentResourceGroup.id}`);
const storageAccount = new azure_native.storage.StorageAccount("storageAccount", {
    accountName: storageAccountName,
    kind: "StorageV2",
    location: location,
    resourceGroupName: currentResourceGroup.apply(currentResourceGroup => currentResourceGroup.name),
    sku: {
        name: storageAccountType,
    },
});
export const accountName = storageAccountName;
export const storageAccountId = storageAccount.id;
```

### Development

The following commands are available which you can run inside the root directory of the repo.

### Build the solution

```bash
dotnet run build 
```

### Run integration tests
```bash
dotnet run integration-tests
```