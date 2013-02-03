SVNCompleteSync
===============

SVNCompleteSync is a C# Subversion client console app which syncs a local directory to a SVN repository. New files are added and deleted files are removed.

It was originally written by HappySpider and hosted at http://svncompletesync.codeplex.com/ . It uses the SharpSvn libraries. http://sharpsvn.open.collab.net/

Changes in my version:
* Adds a checkoutupdate command which will check out or update an SVN URL to a local directory (optionally cleaning the local working copy, or creating the remote URL)
* Improved command-line interface with new options
* Uses newer SharpSvn libraries for SVN 1.7 compatibility


Examples:

Checking out or updating a local working copy from SVN. If the directory does not exist on the SVN server, it will be created. If the local working copy already exists for that SVN URL, it will be cleaned and updated; if it does not exist, it will be checked out.

    svnclient.exe checkoutupdate https://svnserver/svn/projects/mine c:\projects\mine --mkdir --message="Adding new directory for my project" --cleanworkingcopy --verbose --username=svnuser --password=svnpass --trust-server-cert

Committing any changes in the local working copy to SVN. Files added or deleted locally will be added or deleted on the SVN server.

    svnclient.exe completesync --message="Updating my project" c:\projects\mine --verbose --username=svnuser --password=svnpass --trust-server-cert

It works well with https://github.com/bhank/ScriptDB to script SQL databases to SVN.

-Adam Coyne
