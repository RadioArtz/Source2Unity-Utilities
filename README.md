# Source2Unity-Utilities
A collection of utilities related to Importing Source assets into unity via the Source Blender collection.


## Source2Unity Material Fixer

Current featureset:
-Batch assign textures to materials based on full or partial name matching
-Switch between metallic or standard shader
-Functionality for assigning normalmaps based on _normal suffix
-Functionality for fixing importsettings of normalmaps based on _normal suffix

Future plans: 
-Advanced settings section to allow configuring of suffixes and other naming patterns
-Advanced settings section to allow selecting custom shader
-Improved name detection for texture assignment

How to install and use:
-Clone repository and copy its contents into *Assets/Editor* in your unity project.
-Wait for everything to compile. After it's done you should have a new Element on your Toolbar:
![image](https://github.com/RadioArtz/Source2Unity-Utilities/assets/54477532/2428b549-5afa-436b-ba34-24e1bb75c092)

-Select all Materials and drag them onto the empty materials section, same for textures
![Source2Unity matfixer instructions](https://github.com/RadioArtz/Source2Unity-Utilities/assets/54477532/997d773c-e7e8-4ee1-8df8-d477e915cd4a)

-Select your preferences and hit Assign Textures! If you want to you can make changes to your settings and just hit Assign again or you can also Unassign all textures again.
