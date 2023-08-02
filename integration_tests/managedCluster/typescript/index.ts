import * as pulumi from "@pulumi/pulumi";
import * as azure_native from "@pulumi/azure-native";

const config = new pulumi.Config();
// The name of the resource group to operate on
const resourceGroupName = config.require("resourceGroupName");
const currentResourceGroup = azure_native.resources.getResourceGroupOutput({
    resourceGroupName: resourceGroupName,
});
// The name of the Managed Cluster resource.
const clusterName = config.get("clusterName") || "aks101cluster";
// The location of the Managed Cluster resource.
const location = config.get("location") || currentResourceGroup.apply(currentResourceGroup => currentResourceGroup.location);
// Optional DNS prefix to use with hosted Kubernetes API server FQDN.
const dnsPrefix = config.require("dnsPrefix");
// Disk size (in GB) to provision for each of the agent pool nodes. This value ranges from 0 to 1023. Specifying 0 will apply the default disk size for that agentVMSize.
const osDiskSizeGB = config.getNumber("osDiskSizeGB") || 0;
// The number of nodes for the cluster.
const agentCount = config.getNumber("agentCount") || 3;
// The size of the Virtual Machine.
const agentVMSize = config.get("agentVMSize") || "Standard_DS2_v2";
// User name for the Linux Virtual Machines.
const linuxAdminUsername = config.require("linuxAdminUsername");
// Configure all linux machines with the SSH RSA public key string. Your key should include three parts, for example 'ssh-rsa AAAAB...snip...UcyupgH azureuser@linuxvm'
const sshRSAPublicKey = config.require("sshRSAPublicKey");
// Client ID (used by cloudprovider)
const servicePrincipalClientId = config.require("servicePrincipalClientId");
// The Service Principal Client Secret.
const servicePrincipalClientSecret = config.require("servicePrincipalClientSecret");
// The type of operating system.
const osType = config.get("osType") || "Linux";
const cluster = new azure_native.containerservice.ManagedCluster("cluster", {
    agentPoolProfiles: [{
        count: agentCount,
        name: "agentpool",
        osDiskSizeGB: osDiskSizeGB,
        osType: osType,
        vmSize: agentVMSize,
    }],
    dnsPrefix: dnsPrefix,
    linuxProfile: {
        adminUsername: linuxAdminUsername,
        ssh: {
            publicKeys: [{
                keyData: sshRSAPublicKey,
            }],
        },
    },
    location: location,
    resourceGroupName: currentResourceGroup.apply(currentResourceGroup => currentResourceGroup.name),
    resourceName: clusterName,
    servicePrincipalProfile: {
        clientId: servicePrincipalClientId,
        secret: servicePrincipalClientSecret,
    },
});
export const controlPlaneFQDN = cluster.fqdn;
