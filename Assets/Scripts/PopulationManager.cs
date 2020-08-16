﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopulationManager : MonoBehaviour
{
    public GameObject carPrefab;
    public Vector3 startPosition;

    int populationSize = 20;
    float populationTime = 25f;
    float checkAllDeadInterval = 0.5f;
    float timer = 0;
    float timer2 = 0;

    int generationNumber = 1;

    List<GameObject> lastGeneration = new List<GameObject>();
    List<GameObject> currentGeneration = new List<GameObject>();
    List<float> matingPool = new List<float>();

    public List<float> averageFitnesses = new List<float>();
    float matingPoolSum; // sum of all fitnesses in mating pool

    public static float mutationRate = 0.05f;

    void Start()
    {
        CreateStartingGeneration();
    }

    void Update()
    {
        timer += Time.deltaTime;
        timer2 += Time.deltaTime;

        if (timer > checkAllDeadInterval)
        {
            if (EntireGenerationDead()) CreateNewGeneration();
            timer = 0;
        }

        if (timer2 > populationTime)
        {
            CreateNewGeneration();
            timer2 = 0;
        }
    }

    // returns true if all cars in current generation are dead
    bool EntireGenerationDead()
    {
        bool allDead = true;
        foreach (var car in currentGeneration)
        {
            if (!car.GetComponent<CarMovement>().isDead)
            {
                allDead = false;
                break;
            }
        }

        return allDead;
    }

    void CreateStartingGeneration()
    {
        for (int i = 0; i < populationSize; i++)
        {
            // instantiate a car and set it's neural network weights to be equal to the child's
            GameObject t = Instantiate(carPrefab, startPosition, Quaternion.identity);
            NeuralNetwork childNetwork = t.GetComponent<NeuralNetwork>();

            childNetwork.InitializeNetwork();
            childNetwork.RandomizeWeights();

            currentGeneration.Add(t);
        }

        // update the generation text ui
        UIManager.EditText(UIManager.generationText, "Generation: " + generationNumber);
        generationNumber++;
    }

    void CreateNewGeneration()
    {
        // add all elements from the current generation array to the last generation array
        lastGeneration.Clear();
        foreach (var c in currentGeneration) lastGeneration.Add(c);

        timer2 = 0;
        // destroy everything in the last generation
        foreach (var c in currentGeneration) Destroy(c);
        currentGeneration.Clear();

        CreateMatingPool();

        averageFitnesses.Add(matingPoolSum / (float)populationSize);

        // THIS DOESN'T WORK AND IDK WHY
        NeuralNetwork bestInPopulation = GetBestMemberInPopulation();
        GameObject bestT = Instantiate(carPrefab, startPosition, Quaternion.identity);
        NeuralNetwork bestTNetwork = bestT.GetComponent<NeuralNetwork>();
        bestTNetwork.InitializeNetwork();
        bestTNetwork.SetWeights(bestInPopulation);
        currentGeneration.Add(bestT);

        // generate the new generation
        for (int i = 0; i < populationSize-1; i++)
        {
            // find two parents (probability based off fitness) and make a child using crossover
            NeuralNetwork parentA = SelectParent();
            NeuralNetwork parentB = SelectParent();
            NeuralNetwork crossoverChild = Crossover(parentA, parentB);

            // instantiate a car and set it's neural network weights to be equal to the child's
            GameObject t = Instantiate(carPrefab, startPosition, Quaternion.identity);
            NeuralNetwork childNetwork = t.GetComponent<NeuralNetwork>();


            childNetwork.InitializeNetwork();
            childNetwork.SetWeights(crossoverChild);
            childNetwork.Mutate(); // mutate the child a little

            currentGeneration.Add(t);
        }

        // update the generation text ui
        UIManager.EditText(UIManager.generationText, "Generation: " + generationNumber);
        generationNumber++;
    }

    void CreateMatingPool()
    {
        matingPool.Clear();
        matingPoolSum = 0;

        // add every fitness to the mating pool and update the mating pool sum
        foreach (var c in lastGeneration)
        {
            float carFitness = c.GetComponent<CarMovement>().fitness;
            matingPool.Add(carFitness);
            matingPoolSum += carFitness;
        }
    }

    NeuralNetwork GetBestMemberInPopulation()
    {
        float bestFitness = 0f;
        int bestIdx = 0;
        for (int i = 0; i < populationSize; i++)
        {
            if (matingPool[i] > bestFitness) {
                bestFitness = matingPool[i];
                bestIdx = i;
            }
        }

        print("best boi here is " + bestIdx + "with fitness of " + bestFitness);

        return lastGeneration[bestIdx].GetComponent<NeuralNetwork>();
    }

    // selects a parent based off the mating pool
    NeuralNetwork SelectParent()
    {
        float randVal = Random.Range(0f, matingPoolSum); // determines the id of the parent
        float cumulativeSum = 0;

        // determine which interval randVal falls in within the mating pool
        for (int i = 0; i < matingPool.Count; i++)
        {
            cumulativeSum += matingPool[i];
            if (cumulativeSum >= randVal)
                return lastGeneration[i].GetComponent<NeuralNetwork>();
        }

        // print("big problem boi");
        // we will never reach this point (probably)
        return null;
    }

    // takes two parents A and B => returns set's the weights of child
    NeuralNetwork Crossover(NeuralNetwork A, NeuralNetwork B)
    {
        Destroy(GetComponent<NeuralNetwork>());

        NeuralNetwork child = gameObject.AddComponent<NeuralNetwork>();
        child.InitializeNetwork();

        // equal chance of having either parent's biases
        for (int i = 0; i < child.biases.Count; i++)
        {
            for (int j = 0; j < child.biases[i].ColumnCount; j++)
            {
                if (Random.Range(0f, 1f) > 0f) child.biases[i][0, j] = A.biases[i][0, j];
                else child.biases[i][0, j] = B.biases[i][0, j];
            }
            // if (Random.Range(0f, 1f) > 0.5f) {
            //     child.biases[i] = A.biases[i];
            // } else {
            //     child.biases[i] = B.biases[i];
            // }
        }

        // equal chance of having either parent's weights 
        for (int i = 0; i < child.weights.Count; i++)
        {
            for (int j = 0; j < child.weights[i].RowCount; j++)
            {
                for (int k = 0; k < child.weights[i].ColumnCount; k++)
                {
                    child.weights[i][j, k] = Random.Range(-1f, 1f);
                    if (Random.Range(0f, 1f) > 0f) child.weights[i][j, k] = A.weights[i][j, k];
                    else child.weights[i][j, k] = B.weights[i][j, k];
                }
            }

            // if (Random.Range(0f, 1f) > 0.5f) {
            //     child.weights[i] = A.weights[i];
            // } else {
            //     child.weights[i] = B.weights[i];
            // }
        }

        return child;
    }
}
