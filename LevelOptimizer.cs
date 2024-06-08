using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelOptimizer
{
    const float width = 2;//half the level width
    const float height = 2.5f;//half the level height
    const float gridSize = 0.2f;//A grid cell's width

    //Parameters you may want to use, but do not have to use
    const int POPULATION_SIZE = 10;//The size of the population
    const float MUTATION_RATE = 0.5f;//The rate at which to mutate population members
    const int NUM_CROSSOVERS = 500;//The number of crossovers/children to produce
    const int MAX_ATTEMPTS = 200;//The max number of times to iterate the algorithm
    const float threshold = 0.9f;

    //Square obstacle. The points make the assumption that the worldLocation will be the center of a grid cell. 
    private Vector2[] squareObstacle = new Vector2[] { new Vector2(-1 * gridSize * 1.01f / 2f, -1 * gridSize * 1.01f / 2f), new Vector2(gridSize * 1.01f / 2f, -1 * gridSize * 1.01f / 2f), new Vector2(gridSize * 1.01f / 2f, gridSize * 1.01f / 2f), new Vector2(-1 * gridSize * 1.01f / 2f, gridSize * 1.01f / 2f) };


    //TODO; Implement a Genetic Algorithm/Evolutionary Search
    public Level GetLevel(Evaluator evaluator, float goalValue)
    {
        //Instantiate random population
        List<Level> population = new List<Level>();
        for (int i = 0; i < POPULATION_SIZE; i++)
        {
            population.Add(GetRandomLevel());
        }
    
        // genetic algorithm should probably go here
        Level bestCandidateSoFar = null;
        float bestFitnessSoFar = 0;

        int attempts = 0;
        while(bestFitnessSoFar < threshold && attempts < MAX_ATTEMPTS)
        {
            // Mutate population
            for (int populationMember = 0; populationMember<POPULATION_SIZE; populationMember++)
            {
                if (Random.value < MUTATION_RATE)
                {
                    population[populationMember] = Mutate(population[populationMember]);
                }
            }

            // Crossover population
                // use sample to get two parents
                Level parent1 = Sample(population, evaluator, goalValue);
                Level parent2 = Sample(population, evaluator, goalValue);
                // call crossover NUM_CROSSOVER times
                for (int i = 0; i < NUM_CROSSOVERS; i++) {
                    Crossover(parent1, parent2);
                }
            
            // Reduce
            population = Reduce(population, POPULATION_SIZE, evaluator, goalValue);


            attempts += 1;
        
        //Check if we've found the perfect candidate
        foreach (Level l in population)
        {
            float thisFitness = Fitness(l, evaluator, goalValue);
            if (thisFitness > bestFitnessSoFar)
            {
                bestCandidateSoFar = l;
                List<ProtoObstacle> remove = new List<ProtoObstacle>();
                foreach (ProtoObstacle obstacle in l.obstacles)
                {
                    if (obstacle.worldLocation == l.pacmanStartPos | obstacle.worldLocation == l.ghostStartPos)
                    {
                        ProtoPellet newPellet = new ProtoPellet(obstacle.worldLocation);
                        l.pellets.Add(newPellet);
                        remove.Add(obstacle);
                    }
                }
                foreach (ProtoObstacle obstacle in remove)
                {
                    l.obstacles.Remove(obstacle);
                }
                bestFitnessSoFar = thisFitness;
            }
        }
        }


        

        

        Debug.Log("Fitness: " + bestFitnessSoFar);
        Debug.Log("Num Evaluations: " + Evaluator.NUM_EVALUATION_CALLS);//You may not reference Evaluator.NUM_EVALUATION_CALLS outside this line
        return bestCandidateSoFar;
    }

    //Fitness, how close this level is to matching the goal value for this specific evaluator
    //You may wish to change this for the extra credit, but should otherwise not be needed
    public float Fitness(Level l, Evaluator evaluator, float goalValue)
    {
        return 1.0f - Mathf.Abs(evaluator.EvaluateLevel(l) - goalValue);
    }

    //You may want to change this mutate function
    public Level Mutate(Level level)
    {
        Level mutatedLevel = level.Clone();
        //Flip a coin
        for (int i = 0; i < 10; i++)
        {
            if (Random.value > 0.6 & mutatedLevel.pellets.Count != 0)
            {
                //Turn a pellet into an obstacle
                int randomPelletIndex = Random.Range(0, mutatedLevel.pellets.Count - 1);

                ProtoPellet p = mutatedLevel.pellets[randomPelletIndex];

                mutatedLevel.pellets.Remove(p);

                mutatedLevel.obstacles.Add(new ProtoObstacle(p.worldLocation, squareObstacle));
            }
            else
            {
                //Turn an obstacle into a pellet
                if (mutatedLevel.obstacles.Count > 4)
                {
                    int randomObstacleIndex = Random.Range(4, mutatedLevel.obstacles.Count - 1);

                    ProtoObstacle p = mutatedLevel.obstacles[randomObstacleIndex];

                    mutatedLevel.obstacles.Remove(p);

                    //Flip another coin to see if this is a power pellet
                    if (Random.value > 0.5)
                    {
                        //Add a pellet
                        mutatedLevel.pellets.Add(new ProtoPellet(p.worldLocation));
                    }
                    else
                    {
                        //Add a power pellet
                        mutatedLevel.pellets.Add(new ProtoPellet(p.worldLocation, true));
                    }
                }
            }
        }
        return mutatedLevel;
    }

    //You may want to change this crossover function, but its not required for the base assignment
    public Level Crossover(Level parent1, Level parent2)
    {
        Level childLevel = new Level();

        int[] counts = new int[3] {parent1.pellets.Count + parent1.obstacles.Count, parent2.pellets.Count + parent2.obstacles.Count, 0};
        List<Vector3> locations = new List<Vector3>();

        for (int i = 0; i < parent1.pellets.Count; i++)
        {
            if (parent1.pellets[i].worldLocation.x < 0)
            {   
                childLevel.pellets.Add(parent1.pellets[i]);
                Vector3 newLocation1 = parent1.pellets[i].worldLocation;
                locations.Add(newLocation1);
            }
            else
            {
                break;
            }
        }

        for (int i = parent2.pellets.Count - 1; i >= 0; i--)
        {
            if (parent2.pellets[i].worldLocation.x > 0)
            {
                childLevel.pellets.Add(parent2.pellets[i]);
                Vector3 newLocation2 = parent2.pellets[i].worldLocation;
                locations.Add(newLocation2);
            }
            else
            {
                break;
            }
        }

        for (int i = 0; i < 4; i++)
        {
            childLevel.obstacles.Add(parent1.obstacles[i]);
        }

        for (float x = -width + gridSize; x <= width; x += gridSize)
        {
            for (float y = -height + gridSize * 2; y <= height - gridSize; y += gridSize)
            {
                Vector3 location = new Vector3(x, y, 0);
                if (!locations.Contains(location))
                {
                    ProtoObstacle newObstacle = new ProtoObstacle(location, squareObstacle);
                    childLevel.obstacles.Add(newObstacle);
                }
            }
        }

        counts[2] = childLevel.pellets.Count + childLevel.obstacles.Count;

        return childLevel;
    }

    //Probabilistically samples a level from a population based on fitness (this is not an efficient function)
    public Level Sample(List<Level> population, Evaluator evaluator, float goalValue)
    {
        //Calculate fitnesses
        List<float> fitnesses = new List<float>();
        float sumOfFitness = 0;

        for (int i = 0; i < population.Count; i++)
        {
            float fitness = Fitness(population[i], evaluator, goalValue);
            sumOfFitness += fitness;
            fitnesses.Add(fitness);
        }

        //Convert fitnesses into weights for sampling
        for (int i = 0; i < population.Count; i++)
        {
            fitnesses[i] /= sumOfFitness;
        }

        //Sample
        float goalWeight = Random.value;
        float currWeight = 0;
        int k = 0;
        while (currWeight < goalWeight && k < population.Count - 1)
        {
            currWeight += fitnesses[k];
            k += 1;
        }

        return population[k];
    }

    //Returns a reduced population where only the populationSize best levels remain
    private List<Level> Reduce(List<Level> population, int populationSize, Evaluator evaluator, float goalValue)
    {

        foreach (Level l in population)
        {
            float fitness = Fitness(l, evaluator, goalValue);
            l.fitness = fitness;
        }

        population = population.OrderBy(x => x.fitness).ToList();
        population.Reverse();

        List<Level> finalPopulation = new List<Level>();
        for (int i = 0; i < populationSize; i++)
        {
            finalPopulation.Add(population[i]);
        }
        return finalPopulation;
    }

    //Get a random level, useful for initializing a population
    public Level GetRandomLevel()
    {
        Level l = new Level();

        //Borders
        Vector2[] topObstacle = new Vector2[] { new Vector2(-1 * width, height - 0.2f), new Vector2(width, height - 0.2f), new Vector2(width, height - 0.25f), new Vector2(-1 * width, height - 0.25f) };
        l.obstacles.Add(new ProtoObstacle(Vector3.zero, topObstacle));
        Vector2[] rightObstacle = new Vector2[] { new Vector2(width, height - 0.25f), new Vector2(width + 0.05f, height - 0.25f), new Vector2(width + 0.05f, -1 * height + 0.25f), new Vector2(width, -1 * height + 0.25f) };
        l.obstacles.Add(new ProtoObstacle(Vector3.zero, rightObstacle));
        Vector2[] leftObstacle = new Vector2[] { new Vector2(-1 * width, height - 0.25f), new Vector2(-1 * width - 0.05f, height - 0.25f), new Vector2(-1 * width - 0.05f, -1 * height + 0.25f), new Vector2(-1 * width, -1 * height + 0.25f) };
        l.obstacles.Add(new ProtoObstacle(Vector3.zero, leftObstacle));
        Vector2[] downObstacle = new Vector2[] { new Vector2(-1 * width, -1 * height + 0.2f), new Vector2(width, -1 * height + 0.2f), new Vector2(width, -1 * height + 0.25f), new Vector2(-1 * width, -1 * height + 0.25f) };
        l.obstacles.Add(new ProtoObstacle(Vector3.zero, downObstacle));

        float error = Random.Range(0, 100);
        for (float x = width*-1+gridSize; x <= width; x += gridSize)
        {
            for (float y = -1*height + gridSize*2; y <= height-gridSize; y += gridSize)
            {
                //Flip a coin
                if (Mathf.PerlinNoise((x + error) * 2, (y + error) * 2) > 0.5)
                {
                    //Add an obstacle
                    ProtoObstacle newObstacle = new ProtoObstacle(new Vector3(x, y), squareObstacle);
                    l.obstacles.Add(newObstacle);
                }
                else
                {
                    //Flip another coin to see if this is a power pellet
                    if (Random.value > 0.1)
                    {
                        //Add a pellet
                        ProtoPellet newPellet = new ProtoPellet(new Vector3(x, y));
                        l.pellets.Add(newPellet);
                    }
                    else
                    {
                        //Add a power pellet
                        ProtoPellet newPowerPellet = new ProtoPellet(new Vector3(x, y), true);
                        l.pellets.Add(newPowerPellet);
                    }
                }
            }
        }

        return l;
    }
}
