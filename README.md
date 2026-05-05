# TheFeed
A light-weight API to serve media in a social media format. upon your first run it'll create a config file with default values. Close it, and do some congifuration: 

`--debug: Enable debug mode`

`-Config reset: Reset config to default values`

`--DConfig [path]: Set the top directory for images`

`--PConfig [port]: Set the port for the server`

`--IConfig [file]: Set the inspire text file`

`--TConfig [title]: Set the title of the feed`

## Feeds
Feeds are created from a top directory, and treats the top subdirectories as "users". Keep this in mind when setting up a Top Directory. 

⚠️MOST media not inside of a subdirectory will be ignored.⚠️

if you have a an icon within the top directory named `icon.ico`, it will be used as the website's favicon
## Client UI
Within the client directory are 5 UI. The program will tell you the link to use them.

Typically: It's as simple as  `http://<ipaddress>:<ip>/ui/<UIName>` without `.HTML` at the end.

## Custom Captions
if there isn't a file set up you'll be given super generic captions. Drop a file named `inspire.txt` in the directory, and it'll use those. Captions aren't stored in the server's main memory. it's a per feed thing, so if you add a file you don't have to restart the server, just reload the front-end.

## Debug mode
This is a temporary mode for you to see all requests as they come in.

## ⚠️Security Notice⚠️
This server has 0 security protocols other than some file name obfuscation. This also doesn't use HTTPS, so all comunications beteen both server and client are not encrypted.

While it is a "read-only" server, this is NOT ideal for public facing or commercial enviroments.
