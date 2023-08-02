config resourceGroupName "string" {
    description = "The name of the resource group to operate on"
}
currentResourceGroup = invoke("azure-native:resources:getResourceGroup", {
    resourceGroupName = resourceGroupName
})
config storageAccountType "string" {
    description = "Storage Account type"
    default = "Standard_LRS"
}
config location "string" {
    description = "The storage account location."
    default = currentResourceGroup.location
}
config storageAccountName "string" {
    description = "The name of the storage account"
    default = "store${currentResourceGroup.id}"
}
resource storageAccount "azure-native:storage:StorageAccount" {
    kind = "StorageV2"
    location = location
    resourceGroupName = currentResourceGroup.name
    sku = {
        name = storageAccountType
    }
}
output accountName {
    value = storageAccountName
}
output storageAccountId {
    value = storageAccount.id
}
