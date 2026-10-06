# Dealer Database – Starter Solution

Apologies for the mess, the migrations need to be run in this order

20261006085531_Dealers.Designer
20261004131225_ExtendDealerSchema
20261006113046_AddMissingDealerFields


I tested and checked the db with powershell and sqlite3 but you're welcome to use any method to check once the import has been run. 
As with your README you will need to run the import in the same way with:
dotnet run --project src/DealerDatabase.Import

or debugging it through right clicking DealerDatabase.Import -> debug -> Start new instance

I prefer the second method as it allowed me to debug through initial changes made with copilot and identify where it has made mistakes or misinterpreted my intentions.

You can also do the same for the web interface as I'm sure you're aware and it will show you the id and name of all dealers currently included.
