Url_shortener
author: ido latz

This is a URL shortener software:
1) You can enter a url you want to shortener 
2) The software send you a short code that present the url you want to shorten.
3) You search for the code and then you redirect for the original Url.

tech used: C#, http, html.

How To use:
1) search for http://hostIP:8080/
2) enter the URL you want to shorten.
3) search for the given URL --> redirect.

Another Feature:
you can check what your code present by searching for the code
in the designated place inside the http://hostIP:8080/ page,


Installation:
first you need to run this command:
netsh http add urlacl url = http://+:8080/ user=DOMAIN\user
this command add permissions to the program to open a http socket in 8080 port.
then run the file: url_shortener.exe inside his directory.

