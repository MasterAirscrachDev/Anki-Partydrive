# Partydrive Mobile Remote
Partydrive Mobile remote is an application that runs with Partydrive and hosts a mobile web interface to allow for playing partydrive from a mobile device (for users with a laptop but no controller, or for users who want to use their phone as a controller).

# 1. Webserver
It will host its controller webpage and use websockets to communicate between the webpage and the node app.  
This will allow the server to distinctualise clients for matching them to the game.  

# 2. Websocket link
The app will bundle the state data from all connected controllers and send it to ReplayStudios Partydrive via a websocket link.

# 3. Controller webpage
The webpage will be designed to be mobile-friendly and will include easy play controls:
slider for acceleration 
buttons for steering left and right (or optional toggle for gyro controls)
boost button
ability button (with potential support for dynamic icons to show abilites)
Also
showing the player position and other gameplay information like energy level

# 4. QR connection
To allow for easy useage, the app should generate a QR code that will be transmitted to the ReplayStudios Partydrive game and displayed so that users can load the controller webpage on their mobile device by scanning the QR code.