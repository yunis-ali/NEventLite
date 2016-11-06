﻿using System;
using System.Runtime.InteropServices;
using NEventLite.Repository;
using NEventLite.Storage;
using NEventLite_Example.Domain;
using NEventLite_Example.Util;

namespace NEventLite_Example
{
    class Program
    {

        static void Main(string[] args)
        {
            //Load dependency resolver
            var resolver = new DependencyResolver();
            
            //Set snapshot frequency
            resolver.Resolve<ISnapshotStorageProvider>().SnapshotFrequency = 5;

            //Get ioc conatainer to create our repository
            IRepository<Note> rep = resolver.Resolve<IRepository<Note>>();

            Guid SavedItemID = CreateAndDoSomeChanges(rep);

            if (SavedItemID != Guid.Empty)
            {
                LoadAndDisplayPreviouslySaved(SavedItemID, rep);
            }

            Console.WriteLine();
            Console.WriteLine("Press enter key to exit.");

            Console.ReadLine();

        }

        #region Create And Change

        public static Guid CreateAndDoSomeChanges(IRepository<Note> rep)
        {
            Note tmpNote = null;

            //Try to load a given guid. Example: 76bc9edb-9857-4e2a-9fa0-762b90844119
            Console.WriteLine("Enter a GUID to try to load or leave blank and press enter:");
            string strGUID = Console.ReadLine();
            Guid LoadID;

            if ((string.IsNullOrEmpty(strGUID) == false) && (Guid.TryParse(strGUID, out LoadID)))
            {
                tmpNote = LoadNote(LoadID, rep);

                if (tmpNote == null)
                {
                    Console.WriteLine($"No Note found with provided Guid of {LoadID.ToString()}. Press enter key to exit.");
                    return Guid.Empty;
                }
            }
            else
            {
                //Create new note
                tmpNote = CreateNewNote(rep);

                Console.WriteLine("After Creation: This is version 0 of the AggregateRoot.");
                Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(tmpNote));
                Console.WriteLine();
            }

            Console.WriteLine("Doing some changes now...");
            Console.WriteLine("");

            //Do some changes
            DoChanges(tmpNote, rep);

            Console.WriteLine("");
            Console.WriteLine("After Committing Events:");
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(tmpNote));

            return tmpNote.Id;

        }

        private static Note CreateNewNote(IRepository<Note> rep)
        {
            Note tmpNote = new Note("Test Note", "Event Sourcing System Demo", "Event Sourcing");
            rep.Save(tmpNote);
            return tmpNote;
        }

        private static void DoChanges(Note tmpNote, IRepository<Note> rep)
        {
            //Do 3 x 5 events cycle to check snapshots too.
            for (int i = 0; i < 3; i++)
            {
                for (int x = 0; x < 5; x++)
                {
                    tmpNote.ChangeTitle($"Test Note 123 Event ({tmpNote.CurrentVersion + 1})");
                    tmpNote.ChangeCategory($"Event Sourcing in .NET Example. Event ({tmpNote.CurrentVersion + 1})");
                }

                Console.WriteLine($"Committing Changes Now For Cycle {i}");

                //Commit changes to the repository
                rep.Save(tmpNote);
            }

            //Do some changes that don't get caught in the snapshot
            tmpNote.ChangeTitle($"Test Note 123 Event ({tmpNote.CurrentVersion + 1})");
            tmpNote.ChangeCategory($"Event Sourcing in .NET Example. Event ({tmpNote.CurrentVersion + 1})");
            //Commit changes to the repository
            rep.Save(tmpNote);
        }

        #endregion

        #region Load Aggregate

        public static void LoadAndDisplayPreviouslySaved(Guid AggregateID, IRepository<Note> rep )
        {
            //Load same note using the aggregate id
            //This will replay the saved events and construct a new note
            var tmpNoteToLoad = LoadNote(AggregateID, rep);

            Console.WriteLine("");
            Console.WriteLine("After Replaying:");
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(tmpNoteToLoad));
        }

        private static Note LoadNote(Guid NoteID, IRepository<Note> rep)
        {
            var tmpNoteToLoad = rep.GetById(NoteID);
            return tmpNoteToLoad;
        }

        #endregion

    }
}
