# ZoomLauncher (OutSystems ODC)

An **OutSystems Developer Cloud (ODC) External Logic** library that spawns ephemeral AWS EC2 instances to process Zoom Real-Time Meeting Streams (RTMS). To implement the AWS EC2 instance follow to this repo: https://github.com/joaomflcarvalho/ZoomAWSListener


## 🚀 Why this exists?

Zoom RTMS requires a persistent WebSocket connection that stays open for the duration of a meeting (30-60+ minutes).
* **ODC External Logic** (Lambda) has a hard timeout of **15 minutes**.
* **Standard WebHooks** cannot keep a connection open.

**The Solution:** This library allows ODC to trigger a "Disposable Worker" pattern. It launches a tiny EC2 instance (`t3.micro`) that connects to Zoom, processes the stream, pushes data back to ODC, and **automatically terminates itself** when the meeting ends.

## ✨ Features

* **.NET 8 Library:** Compatible with OutSystems ODC.
* **Cost Efficient:** Uses `t3.micro` instances that self-destruct immediately after the meeting.
* **Secure:** Passes credentials via AWS User Data (runtime injection), never stored on the disk image.
* **Automatic Cleanup:** Configured with `InstanceInitiatedShutdownBehavior = Terminate`.

## 🛠️ Architecture

1.  **Zoom** sends a webhook (`meeting.started`) to ODC.
2.  **ODC** calls this library (`LaunchInstance`).
3.  **AWS** spins up a new EC2 instance from a pre-configured AMI.
4.  **The Instance** runs a Node.js script to listen to the WebSocket.
5.  **Meeting Ends** -> WebSocket closes -> Script exits -> Instance runs `shutdown -h now` -> AWS deletes the instance.

## 📋 Prerequisites

1.  **OutSystems ODC** environment.
2.  **AWS Account** with permissions to run EC2 instances.
3.  **Zoom App** (Server-to-Server OAuth) with RTMS enabled.
4.  **A "Golden Image" (AMI)** configured with Node.js and the worker script (see below).

## ⚙️ Setup Guide

### 1. Create the Golden Image (AMI)
You need a Linux AMI that has your listener code ready.
1.  Launch an Amazon Linux 2023 instance.
2.  Install Node.js 18+ and your worker script.
3.  Ensure your script accepts arguments for `MeetingID`, `StreamURL`, etc.
4.  **Crucial:** Your script must run `sudo shutdown -h now` when the WebSocket connection closes.
5.  Save this instance as an AMI and copy the **AMI ID**.

### 2. Configure ODC
In your ODC App, create **Site Properties** (do not hardcode these!):
* `AWSAccessKey`
* `AWSSecretKey`
* `AWS_Region` (e.g., `us-east-1`)
* `AMI_ID` (The ID from Step 1)
* `Subnet_ID` (A public subnet in your VPC)
* `SecurityGroup_ID` (Allowing outbound traffic)

### 3. Usage in ODC
Add this library as a dependency and call the `LaunchInstance` action in your logic flow:

```csharp
LaunchInstance(
    accessKey: Site.AWSAccessKey,
    secretKey: Site.AWSSecretKey,
    region: Site.AWS_Region,
    amiId: Site.AMI_ID,
    subnetId: Site.Subnet_ID,
    securityGroupId: Site.SecurityGroup_ID,
    meetingId: Payload.MeetingUUID,
    streamId: Payload.StreamID,
    ...
)
```

## 📦 Build & Deploy
This project includes a build script to package the library for ODC upload.

```Bash
# MacOS / Linux
./build.sh
Upload the resulting ZoomLauncher.zip to the ODC Portal -> Assets -> Libraries.
```

## 📄 License
MIT

