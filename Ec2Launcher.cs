using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.EC2;
using Amazon.EC2.Model;
using OutSystems.ExternalLibraries.SDK;

namespace ZoomLauncher
{
    [OSInterface(
        Name = "ZoomLauncher", 
        Description = "Launches and manages AWS EC2 instances for Zoom integration",
        IconResourceName = "ZoomLauncher.icon.png")]
    public interface IZoomLauncher
    {
        [OSAction(
            Description = "Launches a new instance.",
            ReturnName = "InstanceId")]
        string LaunchInstance(
            [OSParameter(Description = "AWS Access Key")] string accessKey,
            [OSParameter(Description = "AWS Secret Key")] string secretKey,
            [OSParameter(Description = "Region")] string region,
            [OSParameter(Description = "AMI ID")] string amiId,
            [OSParameter(Description = "Subnet ID")] string subnetId,
            [OSParameter(Description = "Security Group ID")] string securityGroupId,
            [OSParameter(Description = "Meeting UUID")] string meetingId,
            [OSParameter(Description = "Stream ID")] string streamId,
            [OSParameter(Description = "Signaling URL")] string signalingUrl,
            [OSParameter(Description = "Client ID")] string clientId,
            [OSParameter(Description = "Client Secret")] string clientSecret,
            [OSParameter(Description = "ODC API Key")] string odcApiKey,
            [OSParameter(Description = "ODC Callback URL")] string callbackUrl); // <--- NEW INPUT

        [OSAction(
            Description = "Forcefully terminates an EC2 instance.",
            ReturnName = "IsSuccess")]
        bool TerminateInstance(
            string accessKey, string secretKey, string region, string instanceId);
    }

    public class ZoomLauncher : IZoomLauncher
    {
        public string LaunchInstance(
            string accessKey, string secretKey, string region, string amiId, string subnetId, string securityGroupId, 
            string meetingId, string streamId, string signalingUrl, string clientId, string clientSecret, 
            string odcApiKey, string callbackUrl) // <--- NEW INPUT
        {
            var config = new AmazonEC2Config { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region) };
            var ec2Client = new AmazonEC2Client(accessKey, secretKey, config);

            // Updated Startup Script
            // We added the callbackUrl as the LAST argument
            var startupScript = $@"#!/bin/bash
cd /home/ec2-user/zoom-worker

echo ""Starting Zoom Worker for {meetingId}"" >> /home/ec2-user/zoom-debug.log

# ARGUMENTS ORDER:
# 1: MeetingID
# 2: StreamID
# 3: SignalingURL
# 4: ClientID
# 5: ClientSecret
# 6: ODC_API_KEY
# 7: CALLBACK_URL (New!)

su -c ""node worker.js '{meetingId}' '{streamId}' '{signalingUrl}' '{clientId}' '{clientSecret}' '{odcApiKey}' '{callbackUrl}'"" ec2-user

shutdown -h now
";

            string userDataBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(startupScript));

            var request = new RunInstancesRequest
            {
                ImageId = amiId,
                InstanceType = InstanceType.T3Micro,
                MinCount = 1,
                MaxCount = 1,
                SubnetId = subnetId,
                SecurityGroupIds = new List<string> { securityGroupId },
                UserData = userDataBase64,
                NetworkInterfaces = new List<InstanceNetworkInterfaceSpecification>
                {
                    new InstanceNetworkInterfaceSpecification
                    {
                        DeviceIndex = 0,
                        SubnetId = subnetId,
                        Groups = new List<string> { securityGroupId },
                        AssociatePublicIpAddress = true
                    }
                },
                InstanceInitiatedShutdownBehavior = ShutdownBehavior.Terminate,
                TagSpecifications = new List<TagSpecification>
                {
                    new TagSpecification
                    {
                        ResourceType = ResourceType.Instance,
                        Tags = new List<Tag> { new Tag { Key = "Name", Value = $"Zoom-Meeting-{meetingId}" } }
                    }
                }
            };

            var response = ec2Client.RunInstancesAsync(request).Result;
            return response.Reservation.Instances[0].InstanceId;
        }

        public bool TerminateInstance(string accessKey, string secretKey, string region, string instanceId)
        {
            // (Keep existing termination logic exactly as it was)
             var config = new AmazonEC2Config { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region) };
            var ec2Client = new AmazonEC2Client(accessKey, secretKey, config);

            var request = new TerminateInstancesRequest
            {
                InstanceIds = new List<string> { instanceId }
            };

            var response = ec2Client.TerminateInstancesAsync(request).Result;
            var state = response.TerminatingInstances[0].CurrentState.Name;
            return (state == InstanceStateName.ShuttingDown || state == InstanceStateName.Terminated);
        }
    }
}