mathsharp

VS 1.8

A simple math game for kids learning a + _ = 10. Polygons (called AnswerBubbles) move from the top of the screen with numbers containing potential answers to the math problem. If a polygon is clicked (correct or incorrect), the score updates and the polygon is removed for the scene. 
Will be expanding for other modes of play, audio, win conditions, etc. 


To run as normal just run Godot.exe 

To open/run project through Visual Studio. 

Open the .sln file in vs editor. In the Debug menu, go to the Debug Properties menu item for the project. Click the Create a new profile button and choose Executable. In the Executable field, browse to the path of the C# version of the Godot editor, or type %GODOT4% if you have created an environment variable for the Godot executable path (included in project root). It must be the path to the main Godot executable, not the 'console' version. For the Working Directory, type a single period, ., meaning the current directory. Also check the Enable native code debugging checkbox. You may now close this window, click downward arrow on the debug profile dropdown, and select your new launch profile. Hit the green start button, and your game will begin playing in debug mode.
Player data is saved to data.csv and wins.csv in "\AppData\Roaming\Godot\app_userdata\mathsharp" 


Tasks immediate:
1. Animate Chicken
2. Add Chickens to game 


Tasks cont... 
5. add a and b up to 20 and mode name = "a+b=?to20" 

4. adaptive


Sound 
