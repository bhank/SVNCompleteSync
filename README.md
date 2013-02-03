SVNCompleteSync
===============

SVNCompleteSync is a C# Subversion client console app which syncs a local directory to a SVN repository. New files are added and deleted files are removed.

It was originally written by HappySpider and hosted at http://svncompletesync.codeplex.com/ . It uses the SharpSvn libraries. http://sharpsvn.open.collab.net/

Changes in my version:
* Adds a checkoutupdate command which will check out or update an SVN URL to a local directory (optionally cleaning the local working copy, or creating the remote URL)
* Improved command-line interface with new options
* Uses newer SharpSvn libraries for SVN 1.7 compatibility

It works well with https://github.com/bhank/ScriptDB to script SQL databases to SVN.

-Adam Coyne
