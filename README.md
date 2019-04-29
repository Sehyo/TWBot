# TWBot #
Tribalwars Bot
TWBot is a Tribalwars assistant.
Runs 100% in the background.
It currently supports: farming, scavenging, report reading, etc.

# Features #
## Farming Related Features ##
### Intelligent Farming ###
By reading your reports, TWBot saves your farms' resource and wall levels.
The resource levels are used to calculate the optimal interval between farm attacks for each village.
TWBot then uses LA, and only sends out optimal attacks.
TWBot can also send out ram attacks to farm villages with a known wall level.

### Optimal LC Count Simulator ###
TWBot can perform a monte-carlo simulation in order to find the optimal LC count to use for farm attacks.

### Scavenging ###
TWBot supports scavenging for all your villages :)

## Control Related Features ##
### Webpanel and Server Communication ###
TWBot is controllable by a webpanel, that let's you toggle features on and off.
SSH Protocol Communication is used to communicate with the server of the webpanel to get updates.

### Push Notifications ###
TWBot supports sending you push notifications to your phone using the pushover.net service.
We have not yet implemented a method for automatically solving the bot protection captchas for you,
However, TWBot will notify you by push notification when it is time to do the captcha.

## Other Features ##
TWBot will tag incomings for you when it runs.

# TO DO #
1. (DONE) ~~Make Scavenging work in the background (Right now, all features EXCEPT Scavenging functions in the background).~~
2. Make communication to server more robust.
3. Make TWBot work with multiple worlds and accounts, right now structure just allows for one.
4. Make Webpanel prettier.
5. Make the Optimal LC Count Monte Carle Simulator also solve for optimal farm radius. However, this can be computationally expensive. If we give ALL villages the same farm radius, then it is only a 2 unknown variable problem. But, to achieve true optimality we want to find the OPTIMAL radius for EACH village. Effectively making it a very complex problem where we have n + 1 unknown variables, where n = villageCount.
6. Troop Recruiting that follows a template.
7. Building Production.
8. Automatically add new villages to XML file.
9. Automatically remove no longer owned villages from XML file.

# Bugs #
1. Bot Protection Captcha Detection does not trigger if it happens in the mark incomings window.
2. Bot Crashes during tag incomings stage if we have no incomings lol.
3. Server communication is unreliable, if upload is interrupted, then the original file is lost.

# Potential Bugs #
???
