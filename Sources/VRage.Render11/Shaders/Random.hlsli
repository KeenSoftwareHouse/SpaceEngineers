// Source
// http://www.gamedev.net/topic/592001-random-number-generation-based-on-time-in-hlsl/
// Supposebly from the NVidia Direct3D10 SDK
// Slightly modified for my purposes
#define RANDOM_IA 16807
#define RANDOM_IM 2147483647
#define RANDOM_AM (1.0f/float(RANDOM_IM))
#define RANDOM_IQ 127773u
#define RANDOM_IR 2836
#define RANDOM_MASK 123459876

struct RandomGenerator 
{
    int seed; // Used to generate values.

    // Sets the seed
    void SetSeed(const uint value) 
	{
        seed = int(value);
        Cycle();
    }
	
    // Returns the current random float.
    float GetFloat() 
	{
        Cycle();
        return RANDOM_AM * seed;
    }

    // Returns the current random int.
    int GetInt() 
	{
        Cycle();
        return seed;
    }

    // Returns a random float within the input range.
    float GetFloatRange(const float low, const float high) 
	{
        float v = GetFloat();
        return low * ( 1.0f - v ) + high * v;
    }

    // Cycles the generator based on the input count. Useful for generating a thread unique seed.
    // PERFORMANCE - O(N)
    void Cycle(const uint _count) 
	{
        for (uint i = 0; i < _count; ++i)
            Cycle();
    }

    // Generates the next number in the sequence.
    void Cycle() 
	{
        seed ^= RANDOM_MASK;
        int k = seed / RANDOM_IQ;
        seed = RANDOM_IA * (seed - k * RANDOM_IQ ) - RANDOM_IR * k;

        if (seed < 0 ) 
            seed += RANDOM_IM;

        seed ^= RANDOM_MASK;
    }
};
