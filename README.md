# WPFKfmSystem

> This is a simple C# WPF test project.\
> It's a POC for a complete web application about the pork kill floor management industry.\
> The goal is test a system to control kill floor management industry\
> by pork and beef.\

## In Memory Data Base
Singleton class for simulate a data base control by memory.
Using a thread-safe dictionary to store collections of different types.

## Data Seeder
A static class for populate example and test data.

## Repository Patern
All CRUDs are using this patern for manipulate data.

## CRUD Forms
The CRUD forms are divide between two class types.
Here was used herance and generics. 
The idea is try not repeat yourself, and known the WPF tricks off course

### Lists
Show all data from a type of CRUD.

### Forms
Used to create and edit data. 
