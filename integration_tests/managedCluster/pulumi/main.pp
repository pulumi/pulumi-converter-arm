config resourceGroupName "string" {
    description = "The name of the resource group to operate on"
}
currentResourceGroup = invoke("azure-native:resources:getResourceGroup", {
    resourceGroupName = resourceGroupName
})
config clusterName "string" {
    description = "The name of the Managed Cluster resource."
    default = "aks101cluster"
}
config location "string" {
    description = "The location of the Managed Cluster resource."
    default = currentResourceGroup.location
}
config dnsPrefix "string" {
    description = "Optional DNS prefix to use with hosted Kubernetes API server FQDN."
}
config osDiskSizeGB "int" {
    description = "Disk size (in GB) to provision for each of the agent pool nodes. This value ranges from 0 to 1023. Specifying 0 will apply the default disk size for that agentVMSize."
    default = 0
}
config agentCount "int" {
    description = "The number of nodes for the cluster."
    default = 3
}
config agentVMSize "string" {
    description = "The size of the Virtual Machine."
    default = "Standard_DS2_v2"
}
config linuxAdminUsername "string" {
    description = "User name for the Linux Virtual Machines."
}
config sshRSAPublicKey "string" {
    description = "Configure all linux machines with the SSH RSA public key string. Your key should include three parts, for example 'ssh-rsa AAAAB...snip...UcyupgH azureuser@linuxvm'"
}
config servicePrincipalClientId "string" {
    description = "Client ID (used by cloudprovider)"
}
config servicePrincipalClientSecret "string" {
    description = "The Service Principal Client Secret."
}
config osType "string" {
    description = "The type of operating system."
    default = "Linux"
}
resource cluster "azure-native:containerservice:ManagedCluster" {
    agentPoolProfiles = [
        {
            count = agentCount
            name = "agentpool"
            osDiskSizeGB = osDiskSizeGB
            osType = osType
            vmSize = agentVMSize
        }
    ]

    dnsPrefix = dnsPrefix
    linuxProfile = {
        adminUsername = linuxAdminUsername
        ssh = {
            publicKeys = [
                {
                    keyData = sshRSAPublicKey
                }
            ]

        }
    }
    location = location
    resourceGroupName = currentResourceGroup.name
    resourceName = clusterName
    servicePrincipalProfile = {
        clientId = servicePrincipalClientId
        secret = servicePrincipalClientSecret
    }
}
output controlPlaneFQDN {
    value = cluster.fqdn
}
