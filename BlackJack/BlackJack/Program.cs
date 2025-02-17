﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using Casino;
using Casino.BlackJack;

namespace BlackJack
{
    class Program
    {
        static void Main(string[] args)
        {
            // will be instantiated with the chained player constructor
            // if the data type is ever in question, you should declare it. Otherwise you can use var.
            //EXAMPLE: var newPlayer = new Player("Sal");

            //Greet and collect name
            Console.WriteLine("Welcome to the Grand Hotel Casino.\nLet's start by telling me your name.");
            string playerName = Console.ReadLine();

            if (playerName.ToLower() == "admin")
            {
                List<ExceptionEntity> Exceptions = ReadExceptions();
                foreach (var exception in Exceptions)
                {
                    Console.Write(exception.Id + " | ");
                    Console.Write(exception.ExceptionType + " | ");
                    Console.Write(exception.ExceptionMessage + " | ");
                    Console.Write(exception.TimeStamp);
                    Console.WriteLine();
                }
                Console.Read();
                return;
            }

            //collect bank with error handling:
            bool validAnswer = false;
            int bank = 0;
            while (!validAnswer)
            {
                Console.WriteLine("And how much money did you bring today?");
                validAnswer = int.TryParse(Console.ReadLine(), out bank);
                if (!validAnswer) Console.WriteLine("Please enter digits only, no decimals.");
            }
            
            Console.WriteLine("Hello, {0}. Would you like to join a game of TwentyOne?", playerName);
            string answer = Console.ReadLine().ToLower();

            //construct payer and game if they answer 'yes'
            if (answer == "yes" || answer =="yeah" || answer== "y" || answer == "ya")
            {
                Player player = new Player(playerName, bank);
                player.Id = Guid.NewGuid();
                using (StreamWriter file = new StreamWriter(@"C:\Users\KenAn\Desktop\log.txt", true))
                {
                    file.WriteLine(player.Id);
                }
                Game game = new TwentyOneGame();
                game += player;
                player.isActivelyPlaying = true;

                // if the player is active and they have money, continue playing
                while(player.isActivelyPlaying && player.Balance > 0)
                {
                    try
                    {
                        game.Play();
                    }
                    catch (FraudException ex)
                    {
                        Console.WriteLine(ex.Message);
                        UpdateDbWithException(ex);
                        Console.ReadLine();
                        return;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("An error ocurred, please contact a system administrator");
                        UpdateDbWithException(ex);
                        Console.ReadLine();
                        return;
                    }
                }
                game -= player;
                Console.WriteLine("Thank you for playing!");
            }
            //No need for an else statement
            Console.WriteLine("Feel free to look around the casino. Bye for now.");
            Console.ReadLine();
        }

        //Method to log exceptions to DB
        private static void UpdateDbWithException(Exception ex)
        {
            string connectionString = @"Data Source=(localdb)\ProjectsV13;
                                        Initial Catalog=TwentyOneGame;Integrated Security=True;
                                        Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;
                                        ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
            
            string queryString = "INSERT INTO Exceptions (ExceptionType, ExceptionMessage, TimeStamp) VALUES" +
                                "(@ExceptionType, @ExceptionMessage, @TimeStamp)";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                //cast variables to sql datatypes
                SqlCommand command = new SqlCommand(queryString, connection);
                command.Parameters.Add("@ExceptionType", SqlDbType.VarChar);
                command.Parameters.Add("@ExceptionMessage", SqlDbType.VarChar);
                command.Parameters.Add("@TimeStamp", SqlDbType.DateTime);

                //map c# objects to command variables
                command.Parameters["@ExceptionType"].Value = ex.GetType().ToString();
                command.Parameters["@ExceptionMessage"].Value = ex.Message;
                command.Parameters["@TimeStamp"].Value = DateTime.Now;

                connection.Open();
                //NonQuery because it is in insert, not a select
                command.ExecuteNonQuery();
                connection.Close();
            }
        }

        private static List<ExceptionEntity> ReadExceptions()
        {
            string connectionString = @"Data Source=(localdb)\ProjectsV13;
                                        Initial Catalog=TwentyOneGame;Integrated Security=True;
                                        Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;
                                        ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

            string queryString = @"Select Id, ExceptionType, ExceptionMessage, TimeStamp FROM Exceptions";

            //will be our list of exceptions
            List<ExceptionEntity> Exceptions = new List<ExceptionEntity>();

            //create connection with 'using' to guard agianst memory leak
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(queryString, connection);

                connection.Open();
                //Reader loops through each record that is returned
                SqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    //add each datum as a property of ExceptionEntity oject
                    ExceptionEntity exception = new ExceptionEntity();
                    exception.Id = Convert.ToInt32(reader["Id"]);
                    exception.ExceptionType = reader["ExceptionType"].ToString();
                    exception.ExceptionMessage = reader["ExceptionMessage"].ToString();
                    exception.TimeStamp = Convert.ToDateTime(reader["TimeStamp"]);
                    Exceptions.Add(exception);
                }
                connection.Close();
            }
            return Exceptions;
        }
    }
}
