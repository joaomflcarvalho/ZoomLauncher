using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.EC2;
using Amazon.EC2.Model;
using OutSystems.ExternalLibraries.SDK; // Required for ODC

namespace ZoomLauncher
{
    // 1. THE CONTRACT (What ODC sees)
    [OSInterface(
        Name = "ZoomLauncher", 
        Description = "Launches AWS EC2 instances for Zoom RTMS integration")] // Icon is optional
    public interface IZoomLauncher
    {
        [OSAction(
            Description = "Spins up a new EC2 instance from a Golden Image to listen to a Zoom meeting.",
            ReturnName = "InstanceId")]
        string LaunchInstance(
            [OSParameter(Description = "AWS Access Key ID")] string accessKey,
            [OSParameter(Description = "AWS Secret Access Key")] string secretKey,
            [OSParameter(Description = "AWS Region (e.g., us-east-1)")] string region,
            [OSParameter(Description = "The ID of your Golden Image (AMI)")] string amiId,
            [OSParameter(Description = "VPC Subnet ID")] string subnetId,
            [OSParameter(Description = "Security Group ID")] string securityGroupId,
            [OSParameter(Description = "Zoom Meeting UUID")] string meetingId,
            [OSParameter(Description = "Zoom Stream ID")] string streamId,
            [OSParameter(Description = "Zoom Signaling URL")] string signalingUrl,
            [OSParameter(Description = "Zoom Client ID")] string clientId,
            [OSParameter(Description = "Zoom Client Secret")] string clientSecret,
            [OSParameter(Description = "ODC API Key for callback")] string odcApiKey);
    }

    // 2. THE IMPLEMENTATION ( The Logic)
    public class ZoomLauncher : IZoomLauncher
    {
        public string LaunchInstance(
            string accessKey, 
            string secretKey, 
            string region, 
            string amiId, 
            string subnetId, 
            string securityGroupId, 
            string meetingId, 
            string streamId, 
            string signalingUrl, 
            string clientId, 
            string clientSecret,
            string odcApiKey)
        {
            // Configure AWS Client
            var config = new AmazonEC2Config { RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region) };
            var ec2Client = new AmazonEC2Client(accessKey, secretKey, config);

            // Create the Startup Script (User Data)
            var startupScript = $@"#!/bin/bash
cd /home/ec2-user/zoom-worker

# Log start
echo ""Starting Zoom Worker for {meetingId}"" >> /home/ec2-user/zoom-debug.log

# Run the worker as 'ec2-user'
# Arguments passed: MeetingID, StreamID, SignalingURL, ClientID, ClientSecret, ODC_API_KEY
su -c ""node worker.js '{meetingId}' '{streamId}' '{signalingUrl}' '{clientId}' '{clientSecret}' '{odcApiKey}'"" ec2-user

# Shutdown when finished
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
                
                // Public IP required for Zoom connection
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

                // Terminate on shutdown to save money
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

            // Execute synchronously
            var response = ec2Client.RunInstancesAsync(request).Result;
            
            // Return the new Instance ID
            return response.Reservation.Instances[0].InstanceId;
        }
    }
}