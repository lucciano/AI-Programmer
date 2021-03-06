﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AIProgrammer.GeneticAlgorithm;
using AIProgrammer.Repository.Interface;
using RSSAutoGen.Repository.Concrete;
using AIProgrammer.Types;

namespace AIProgrammer
{
    /// <summary>
    /// AIProgrammer experiment, using artificial intelligence to generate a program that solves a solution.
    /// This experiment uses a genetic algorithm to evolve a program in the programming language Brainfuck.
    /// The resulting program will print a target string.
    ///
    /// Created by Kory Becker 01-Jan-2013 http://www.primaryobjects.com/kory-becker.aspx
    /// </summary>
    class Program
    {
        private static GA _ga = null; // Our genetic algorithm instance.
        private static double _bestFitness = 0; // Best fitness so far.
        private static string _bestProgram = ""; // Best program so far.
        private static string _bestOutput = ""; // Best program output so far.
        private static int _bestIteration = 0; // Current iteration (generation) count.
        private static int _bestTicks = 0; // Number of instructions executed by the best program.
        private static bool _bestNoErrors = false; // Indicator if the program had errors or not.
        private static DateTime _bestLastChangeDate = DateTime.Now; // Time of last improved evolution.
        private static DateTime _startTime = DateTime.Now; // Time the program was started.

        private static double _crossoverRate = 0.70; // Percentage chance that a child genome will use crossover of two parents.
        private static double _mutationRate = 0.01; // Percentage chance that a child genome will mutate a gene.
        private static int _genomeSize = 100; // Number of programming instructions in generated program (size of genome array).
        private static int _maxIterationCount = 2000; // Max iterations a program may run before being killed (prevents infinite loops).
        private static string _targetString = "hi"; // Target string to generate a program to print.

        /// <summary>
        /// Event handler that is called upon each generation. We use this opportunity to display some status info and save the current genetic algorithm in case of crashes etc.
        /// </summary>
        private static void OnGeneration(GA ga)
        {
            if (_bestIteration++ > 1000)
            {
                _bestIteration = 0;
                Console.WriteLine("Best Fitness: " + _bestFitness + "/" + ga.GAParams.TargetFitness + " " + Math.Round(_bestFitness / ga.GAParams.TargetFitness * 100, 2) + "%, Ticks: " + _bestTicks + ", Running: " + Math.Round((DateTime.Now - _startTime).TotalMinutes) + "m, Best Output: " + _bestOutput + ", Changed: " + _bestLastChangeDate.ToString() + ", Program: " + _bestProgram);

                ga.Save("my-genetic-algorithm.dat");
            }
        }

        /// <summary>
        /// Fitness function to evaluate the current genetic algorithm. We decode the weights, run the resulting program, and score the output.
        /// </summary>
        /// <param name="weights">Array of double (genes), where each value cooresponds to a Brainfuck program command.</param>
        /// <returns>double, indicating the score</returns>
        private static double fitnessFunction(double[] weights)
        {
            double fitness = 0;
            StringBuilder console = new StringBuilder();
            bool noErrors = false;
            Interpreter bf = null;

            // Get the resulting Brainfuck program.
            string program = ConvertDoubleArrayToBF(weights);

            try
            {
                // Run the program.
                bf = new Interpreter(program, null, (b) =>
                {
                    console.Append((char)b);

                    // If we've printed out more than our target string, then kill the program. This saves us the time of running the remaining iterations. Ok maybe this is cheating, but at least it's still in the fitness function.
                    if (console.Length >= _targetString.Length)
                    {
                        bf.m_Stop = true;
                    }
                });
                bf.Run(_maxIterationCount);

                // It runs!
                noErrors = true;
            }
            catch
            {
            }

            // Order bonus.
            for (int i = 0; i < _targetString.Length; i++)
            {
                if (console.Length > i)
                {
                    fitness += 256 - Math.Abs(console[i] - _targetString[i]);
                }
            }

            // Is this a new best fitness?
            if (fitness > _bestFitness)
            {
                _bestFitness = fitness;
                _bestOutput = console.ToString();
                _bestNoErrors = noErrors;
                _bestLastChangeDate = DateTime.Now;
                _bestProgram = program;
                _bestTicks = bf.m_Ticks;
            }

            return fitness;
        }

        /// <summary>
        /// Convert a genome (array of doubles) into a Brainfuck program.
        /// </summary>
        /// <param name="array">Array of double</param>
        /// <returns>string - Brainfuck program</returns>
        private static string ConvertDoubleArrayToBF(double[] array)
        {
            StringBuilder sb = new StringBuilder();

            foreach (double d in array)
            {
                if (d <= 0.125) sb.Append('>');
                else if (d <= 0.25) sb.Append('<');
                else if (d <= 0.375) sb.Append('+');
                else if (d <= 0.5) sb.Append('-');
                else if (d <= 0.625) sb.Append('.');
                else if (d <= 0.75) sb.Append(',');
                else if (d <= 0.875) sb.Append('[');
                else sb.Append(']');
            }

            return sb.ToString();
        }

        /// <summary>
        /// Setup the genetic algorithm and run it.
        /// </summary>
        /// <returns>Array of double, the best brain's output</returns>
        private static double[] Setup()
        {
            // Genetic algorithm setup.
            _ga = new GA(_crossoverRate, _mutationRate, 100, 10000000, _genomeSize);

            // Start a new genetic algorithm.
            _ga.GAParams.Elitism = true;
            _ga.GAParams.TargetFitness = _targetString.Length * 256;
            _ga.GAParams.HistoryPath = System.IO.Directory.GetCurrentDirectory() + "\\history.txt";
            _ga.FitnessFunction = new GAFunction(fitnessFunction);
            _ga.OnGenerationFunction = new OnGeneration(OnGeneration);
            _ga.Go();

            // Load a saved genetic algorithm.
            //_ga.Load("my-genetic-algorithm.dat");
            //_ga.Resume(fitnessFunction, OnGeneration);

            // Results.
            double[] weights;
            double fitness;
            _ga.GetBest(out weights, out fitness);
            Console.WriteLine("***** DONE! Best brain had a fitness of " + fitness);

            // Save the result.
            //_ga.Save("my-genetic-algorithm.dat");

            return weights;
        }

        /// <summary>
        /// Main program.
        /// </summary>
        static void Main(string[] args)
        {
            // Run the genetic algorithm and get the best brain.
            double[] output = Setup();

            // Convert the best brain's output into a program.
            string program = ConvertDoubleArrayToBF(output);
            Console.WriteLine(program);
            Console.WriteLine("------");

            try
            {
                // Run the program.
                Interpreter bf = new Interpreter(program, null, (b) =>
                {
                    Console.Write((char)b);
                });

                bf.Run(_maxIterationCount);
            }
            catch
            {
            }


            Console.ReadKey();
        }
    }
}
