# NoEyeDeer Sandbox

Main Goal: 
Develop a system for realistic physical bullet for various types of bullets that implements real world forces in unity. Where this can be published into unity store for others to implement into their projects. 

Our contributions :

Jacob Tang (Main Brunch on github) : 

Developed the script that for bullet physics. Bullet class script contains the following features (Allows the user to initialize any bullet of any caliber . This is due to the implementation of drag model (G1, G2, G5, G6, G7, G8, GS)  used by the US Army) . The script as of its current form implements almost all aspects of bullet physics ( Gravity , Drag , Spin Drift , Coriolis , Centripetal , Wind ) and taking into account of (Firing angle , temperature , pressure , rifle twist , firing azimuth north south referenced ,  and latitude )

Gun shooting (Bullet spawn) system.

Created the Map. 

Tatsuya (Dragon Brunch on github) :
Movement (gravity, jump) Enemy Installed monster asset Added state machine ( idle state, patrol state, chase state, attack state, die state) Added taking damage on the monster Added health bar Added random respawn

John (Main Brunch on github)
Player Movement, Player Camera, Camera Shake, Gun Swaying, Jump, Crouching, Sprinting Weapon Holder, Sniper Weapon, Dual Render Scope, Gun Particles, Gun shooting, Crosshair Ammo UI, Muzzle Flash, and scoping effects Scripts: Camera Shake, Mouse Look (Player look), fixed player movement, recoil, and weapon sway.
