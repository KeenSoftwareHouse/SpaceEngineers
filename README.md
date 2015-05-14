Space Engineers
===============

Welcome to the Space Engineers source code! 

From this repository you can build Space Engineers. To play Space Engineers you need to own the game on Steam. Assets (audio, models, textures) are not included in this repository.

Before downloading the source code please read carefully
[End User License Agreement](https://github.com/KeenSoftwareHouse/SpaceEngineers/blob/master/EULA.txt)

See [Change log](https://github.com/KeenSoftwareHouse/SpaceEngineers/wiki/Change-log) for latest changes.   
Have you found a problem related to source codes? Report an [Issue](https://github.com/KeenSoftwareHouse/SpaceEngineers/issues).   
Discuss source code on our [forum](http://forum.keenswh.com/forums/source-code.423135/).

Prerequisities
--------------
- [Visual Studio 2013 Community Edition] (https://www.visualstudio.com/en-us/downloads/download-visual-studio-vs#d-community) or different version of VS2013
- Steam Client + Space Engineers game (to run and test the game)

Quickstart
----------
Space Engineers must be installed on your computer, Steam must be running.

- [Clone](github-windows://openRepo/https://github.com/KeenSoftwareHouse/SpaceEngineers) or [download](https://github.com/KeenSoftwareHouse/SpaceEngineers/archive/master.zip) and unpack the repository.
- Open **SpaceEngineers.sln** in Visual Studio.
- Open file **global.props** (it's in configuration folder).
- Make sure **ContentPath** tag contains path to SpaceEngineers **Content** directory in Steam folder.
- Start debugging by pressing **F5** or select **Debug** - **Start Debugging** in main menu

Instead of modifying **global.props**, you can create **user.props**, more information [here](https://github.com/KeenSoftwareHouse/SpaceEngineers/wiki/Initial-setup).

How to contribute
-----------------

One way to contribute changes is to send a GitHub [Pull Request](https://help.github.com/articles/using-pull-requests).

**To get started using GitHub:**

- Create your own Space Engineers **fork** by clicking the __Fork button__ in the top right of this page.
- [Install a Git client](http://help.github.com/articles/set-up-git) on your computer.
- Use the GitHub program to **Sync** the project's files to a folder on your computer.
- Open up **SpaceEngineers.sln** in Visual Studio.
- Modify the source codes and test your changes.
- Using the GitHub program, you can easily **submit contributions** back up to your **fork**.  These files will be visible to all subscribers.
- When you're ready to send the changes to the Keen Software House for review, simply create a [Pull Request](https://help.github.com/articles/using-pull-requests).

Where is 64-bit version?
------------------------

We're unable to provide 64-bit version of all 3rd party libraries because of licensing. We're working on this and trying to negotiate better license which will allows us to do that.
