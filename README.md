# Unity-AI-AR
WIP including Medium Post

![alt text][shespizza]

## Configuring Watson Services
This section is broken up into two different processes, one for Speech-To-Text and Text-To-Speech, the other for Conversation (as it requires a little more work).

### Speech-to-Text and Text-to-Speech
Create an IBM Cloud account if you haven't already.

Click "Catalog" in the upper menu. This will take you to the list of available services, platforms, and other offerings on the IBM Cloud. To make it simple, use the column menu and click "Watson" at the very bottom. This will filter the catalog to just show Watson Services.

![alt text][WatsonCatalogOfferings]

Click on either Speech-to-Text or Text-to-Speech to start the provisioning of the service. The steps below will outline the process for Speech-to-Text but will be identical for Text-to-Speech.

Name the service, and select a region/location to deploy in. If applicable, choose an organization and space or leave it to the default if you only have one of each.

![alt text][stt-top]

For pricing plans, keep the Lite plan, but notice the first 100 minutes are free per month.

![alt text][stt-bottom]

Click "Create" and wait for the service to provision. This may take some time.

Once the service is provisioned successfully, click on Service credentials.

![alt text][stt-manage]

Expand "View credentials" to reveal your username and password. Enter these as strings in the C# script to access the API.

![alt text][stt-service-credentials]


## Resources
https://unity3d.com/learn/tutorials/topics/mobile-touch/building-your-unity-game-ios-device-testing
http://www.vergium.com/image-and-ar-camera-step-your-first-ar-app-in-20-minutes/
https://library.vuforia.com/articles/Solution/Smart-Terrain-Workflow-in-Unity
https://library.vuforia.com/content/vuforia-library/en/articles/Solution/Working-with-Vuforia-and-Unity.html
https://github.com/watson-developer-cloud/unity-sdk

[WatsonCatalogOfferings]: ./images/WatsonCatalogOfferings.png "alt text"
[stt-top]: ./images/stt-top-provision.png "alt text"
[stt-bottom]: ./images/stt-bottom-provision.png "alt text"
[shespizza]: ./images/shespizza.png "alt text"
[stt-manage]: ./images/stt-manage.png "alt text"
[stt-service-credentials]: ./images/stt-service-credentials.png "alt text"
