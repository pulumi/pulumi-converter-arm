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
