﻿using Sudoku.Shared;
using System.Linq;
using Microsoft.ML.Probabilistic.Algorithms;
using Microsoft.ML.Probabilistic.Distributions;
using Microsoft.ML.Probabilistic.Math;
using Microsoft.ML.Probabilistic.Models;
using Microsoft.ML.Probabilistic.Models.Attributes;
using System.Collections.Generic;

namespace Sudoku.Probabilistic
{

    public class ProbabilisticSolver : Sudoku.Shared.ISolverSudoku
    {

        private static RobustSudokuModel robustModel = new RobustSudokuModel();

        GridSudoku ISolverSudoku.Solve(GridSudoku s)
        {
            return NaiveSudokuModel.SolveSudoku(s);
        }
    }

    /*
    public class RobustSudokuModel
    {

        public InferenceEngine InferenceEngine;

        private static List<int> CellDomain = Enumerable.Range(1, 9).ToList();
        private static List<int> CellIndices = Enumerable.Range(0, 81).ToList();

        public VariableArray<Dirichlet> CellsPrior;
        public VariableArray<Vector> ProbCells;
        public VariableArray<int> Cells;

        private const double EpsilonProba = 0.00000001;
        private static double FixedValueProba = 1.0 - ((CellDomain.Count - 1) * EpsilonProba);

        public RobustSudokuModel()
        {


            Range valuesRange = new Range(CellDomain.Count).Named("valuesRange");
            Range cellsRange = new Range(CellIndices.Count).Named("cellsRange");


            CellsPrior = Variable.Array<Dirichlet>(cellsRange).Named("CellsPrior");
            ProbCells = Variable.Array<Vector>(cellsRange).Named("ProbCells");
            ProbCells[cellsRange] = Variable<Vector>.Random(CellsPrior[cellsRange]);
            ProbCells.SetValueRange(valuesRange);


            // Initialisation des distribution a priori de façon uniforme (les valeurs sont équiprobables pour chaque cellule)

            Dirichlet[] dirUnifArray =
                Enumerable.Repeat(Dirichlet.Uniform(CellDomain.Count), CellIndices.Count).ToArray();
            CellsPrior.ObservedValue = dirUnifArray;

            Cells = Variable.Array<int>(cellsRange);
            Cells[cellsRange] = Variable.Discrete(ProbCells[cellsRange]);



            //Ajout des contraintes de Sudoku (all diff pour tous les voisinages)
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    foreach (var neighbourCellIndex in GridSudoku.CellNeighbours[i][j])
                    {
                        if (neighbourCellIndex > (i,j))
                        {
                            Variable.ConstrainFalse(Cells[cellIndex] == Cells[neighbourCellIndex]);
                        }
                    }
                }
            }

            //Todo: tester d'autres algo et paramétrages associés

            IAlgorithm algo = new ExpectationPropagation();
            //IAlgorithm algo = new GibbsSampling();
            //IAlgorithm algo = new VariationalMessagePassing();
            //IAlgorithm algo = new MaxProductBeliefPropagation();
            //les algos ont ete teste cependant la difference lors du lancement du projet n'est pas facilement visible
            algo.DefaultNumberOfIterations = 50;
            //algo.DefaultNumberOfIterations = 200;


            InferenceEngine = new InferenceEngine(algo);

            //InferenceEngine.OptimiseForVariables = new IVariable[] { Cells };

        }


        public virtual void SolveSudoku(GridSudoku s)
        {
            Dirichlet[] dirArray = Enumerable.Repeat(Dirichlet.Uniform(CellDomain.Count), CellIndices.Count).ToArray();

            //On affecte les valeurs fournies par le masque à résoudre en affectant les distributions de probabilités initiales
            foreach (var cellIndex in GridSudoku.IndicesCellules)
            {
                if (s.Cellules[cellIndex] > 0)
                {

                    //Vector v = Vector.Zero(CellDomain.Count);
                    //v[s.Cellules[cellIndex] - 1] = 1.0;


                    //Todo: Alternative: le fait de mettre une proba non nulle permet d'éviter l'erreur "zero probability" du Sudoku Easy-n°2, mais le Easy#3 n'est plus résolu
                    //tentative de changer la probabilite pour solver le sudoku 3 infructueuse
                    Vector v = Vector.Constant(CellDomain.Count, EpsilonProba);
                    v[s.Cellules[cellIndex] - 1] = FixedValueProba;

                    dirArray[cellIndex] = Dirichlet.PointMass(v);
                }
            }

            CellsPrior.ObservedValue = dirArray;

            //DistributionRefArray<Discrete, int> cellsPosterior = (DistributionRefArray<Discrete, int>)InferenceEngine.Infer(Cells);
            //var cellValues = cellsPosterior.Point.Select(i => i + 1).ToList();

            //Autre possibilité de variable d'inférence (bis)
            Dirichlet[] cellsProbsPosterior = InferenceEngine.Infer<Dirichlet[]>(ProbCells);

            foreach (var cellIndex in GridSudoku.IndicesCellules)
            {
                if (s.Cellules[cellIndex] == 0)
                {

                    //s.Cellules[cellIndex] = cellValues[cellIndex];


                    var mode = cellsProbsPosterior[cellIndex].GetMode();
                    var value = mode.IndexOf(mode.Max()) + 1;
                    s.Cellules[cellIndex] = value;
                }
            } */



    // <summary>
    /// Ce premier modèle est très faible: d'une part, il ne résout que quelques Sudokus faciles, d'autre part, le modèle est recompilé à chaque fois, ce qui prend beaucoup de temps
    /// </summary>
    ///

    public class NaiveSudokuModel
    {

        private static List<int> CellDomain = Enumerable.Range(1, 9).ToList();
        private static List<int> CellIndices = Enumerable.Range(0, 81).ToList();


        public virtual void SolveSudoku(GridSudoku grille)
        {

            var algo = new ExpectationPropagation();
            var engine = new InferenceEngine(algo);

            //Implémentation naïve: une variable aléatoire entière par cellule
            var cells = new List<Variable<int>>(CellIndices.Count);

            foreach (var cellIndex in GridSudoku.IndicesCellules)
            {
                //On initialise le vecteur de probabilités de façon uniforme pour les chiffres de 1 à 9
                var baseProbas = Enumerable.Repeat(1.0, CellDomain.Count).ToList();
                //Création et ajout de la variable aléatoire
                var cell = Variable.Discrete(baseProbas.ToArray());
                cells.Add(cell);

            }

            //Ajout des contraintes de Sudoku (all diff pour tous les voisinages)
            foreach (var cellIndex in GridSudoku.IndicesCellules)
            {
                foreach (var neighbourCellIndex in GridSudoku.VoisinagesParCellule[cellIndex])
                {
                    if (neighbourCellIndex > cellIndex)
                    {
                        Variable.ConstrainFalse(cells[cellIndex] == cells[neighbourCellIndex]);
                    }
                }
            }

            //On affecte les valeurs fournies par le masque à résoudre comme variables observées
            foreach (var cellIndex in GridSudoku.IndicesCellules)
            {
                if (grille.Cellules[cellIndex] > 0) //pour toutes les cellules de la grille, si la cellule est > 0 
                {
                    cells[cellIndex].ObservedValue = grille.Cellules[cellIndex] - 1; //soustraire 1 à la valeur de cette cellule
                }
            }

            foreach (var cellIndex in GridSudoku.IndicesCellules)
            {
                if (grille.Cellules[cellIndex] == 0) //pour toutes les cellules de la grille, si la cellule est = 0
                {
                    var result = (Discrete)engine.Infer(cells[cellIndex]); //creation d'une variable resultat
                    grille.Cellules[cellIndex] = result.Point + 1; //additionner 1 à resultat
                }
            }
        }
    }
}