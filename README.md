# Kitty Service Fabric

This project is a small example of a front end API and backend stateful service for service fabric.  It gets kitty pictures via (placekitten.com) and will cache them in an Azure container with an md5 reference persisted in your backend stateful service.

## Getting Started

These instructions will get you a copy of the project up and running on your local machine for development and testing purposes. See deployment for notes on how to deploy the project on a live system.

### Prerequisites

- [Service Fabric SDK}(https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-get-started)
- Azure Storage Account -or- [Azure Storage Emulator](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator)
- Visual Studio or MSBuild and a Text Editor
- .NET Core 2.+

Microsoft provides a [free Service Fabric cluster](https://try.servicefabric.azure.com/) if you want to try it there too.

### Installing

Once cloned, you'll have to provide your connection string to your storage account.  To get this, go into your Storage Account and click on Access Keys.  you'll find "Connection String" at the bottom of the list.

## Using Azure Storage Account
Copy and paste that into KittyGotBack/PackageRoot/ServiceManifest.xml:
```
  <CodePackage Name="Code" Version="1.0.0">
    <EntryPoint>
      <ExeHost>
        <Program>KittyGotBack.exe</Program>
      </ExeHost>
    </EntryPoint>
    <EnvironmentVariables>
      <EnvironmentVariable Name="containerConnectionString" Value="PASTE HERE!"/>
    </EnvironmentVariables>
  </CodePackage>
```
## Using Azure Storage Emulator
A new alternative to using Azure Storage is to use the emulator. Download and instructions on setting up can be found [here](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-emulator)

Once you run init and start, the only config changes you need to make (provided you used default settings) is to set the connection string to set the variable mentioned above to:
```
<EnvironmentVariable Name="containerConnectionString" Value="UseDevelopmentStorage=true"/>
```
---
Once your connection string is in there, you can debug or deploy this application to see it's wonderous effects.  

To debug locally, choose BuildConfiguration Debug and Platform x64.

Use your favorite REST tool like Postman to perform a GET on http://localhost:8943/api/kitties/300/300

The url path spec is /api/kitties/{width}/{height}

Change those numbers to get different sized kitty pictures.

## Running the tests

No Tests for this, sorry.

## Deployment

Deployment is outside the scope of this demo.  See Microsoft Documentation for more information.
[Deploy and remove applications using PowerShell](https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-deploy-remove-applications)
[Tutorial: deploy an application with CI/CD to a Service Fabric cluster](https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-tutorial-deploy-app-with-cicd-vsts)

## Built With

* [Dropwizard](http://www.dropwizard.io/1.0.2/docs/) - The web framework used
* [Maven](https://maven.apache.org/) - Dependency Management
* [ROME](https://rometools.github.io/rome/) - Used to generate RSS Feeds

## Contributing

Please read [CONTRIBUTING.md](https://gist.github.com/PurpleBooth/b24679402957c63ec426) for details on our code of conduct, and the process for submitting pull requests to us.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details

## Acknowledgments

* This couldn't have been possible without Microsoft and placekitten.com
