# From PLC to Model

This project demonstrates how to automatically create Model tags which are then linked to the imported PLC tags, this is very convenient when the user needs to create an abstract representation of the PLC tags (when using multiple PLC manufacturer with same tag structure for example)

## Deprecated

This script is being distributed in the 1.4.x "Scripts" TemplateLibrary

## Execution

1. Import the PLC tags to the project
1. Configure the `FromPlcToModel/StartingNodeToFetch` variable to the root node containing your PLC tag(s) (for example: `CommDrivers/RAEtherNet_IPDriver1/RAEtherNet_IPStation1/Tags`)
1. Configure the `FromPlcToModel/TargetFolder` variable to the root node where you want your Model tags to be created (for example: `Model/Station1`)
1. Execute the `FromPlcToModel/GenerateNodesIntoModel` method to start processing

### Additional configuration

- User can enable the `FromPlcToModel/DeleteExistingTags` variable to cleanup the target node root path before creating the new tags

## Disclaimer

Rockwell Automation maintains these repositories as a convenience to you and other users. Although Rockwell Automation reserves the right at any time and for any reason to refuse access to edit or remove content from this Repository, you acknowledge and agree to accept sole responsibility and liability for any Repository content posted, transmitted, downloaded, or used by you. Rockwell Automation has no obligation to monitor or update Repository content

The examples provided are to be used as a reference for building your own application and should not be used in production as-is. It is recommended to adapt the example for the purpose, observing the highest safety standards.
