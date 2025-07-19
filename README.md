mathsharp

VS 1.8

A simple math game for kids learning a + _ = 10. Polygons (called AnswerBubbles) move from the top of the screen with numbers containing potential answers to the math problem. If a polygon is clicked (correct or incorrect), the score updates and the polygon is removed for the scene. 
Will be expanding for other modes of play, audio, win conditions, etc. 

You will need godotsharp to run this project. It is not included in the repo for size reasons. Just open project.godot from you godot editor to run or build. 

To open/run project through Visual Studio. 

Open the .sln file in vs editor. In the Debug menu, go to the Debug Properties menu item for the project. Click the Create a new profile button and choose Executable. In the Executable field, browse to the path of the C# version of the Godot editor, or type %GODOT4% if you have created an environment variable for the Godot executable path. It must be the path to the main Godot executable, not the 'console' version. For the Working Directory, type a single period, ., meaning the current directory. Also check the Enable native code debugging checkbox. You may now close this window, click downward arrow on the debug profile dropdown, and select your new launch profile. Hit the green start button, and your game will begin playing in debug mode.
Player data is saved to data.csv and wins.csv in "\AppData\Roaming\Godot\app_userdata\mathsharp" 

Anything further and you're on your own. 

Tasks immediate:
1. Add adaptive mode 
	make a problem set of the slowest 20 problems 
	set the time for them to the average time it took for the problems times 20 

