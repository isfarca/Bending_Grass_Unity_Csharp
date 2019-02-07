
#ifndef BENDING_INCLUDED
#define BENDING_INCLUDED

#define MAX_BENDERS 63




sampler3D _volumeMap;		// The current 3d texture that contains the volume voxel data
float4 _volumeMapTexSize;	// The current 3d texture resolution (w unused)
float4 _volumeOrigin;		// The origin of the volume x,y,z (w unused)
float4 _volumeSize;			// The world space size of the volume x,y,z (w unused)

float _volumeBendMult;		// Maximum grass bend distance for permanent bending

// Convert world space position to normalized volume position (0-1 range)
float3 WorldToVoxelSpace(float3 worldPos)
{
	return ((worldPos - _volumeOrigin) / _volumeSize);
}

// Sample the voxel value at the specific world position (x,y,z is direction, w is multiplier factor)
float4 SampleVoxel(float3 worldPos)
{
	float3 voxSpace = WorldToVoxelSpace(worldPos);

	//We don't want the 3d grid to affect things outside of its volume
	if (voxSpace.x > 1 || voxSpace.x < 0)
		return float4(.5, .5, .5, 0);
	if (voxSpace.y > 1 || voxSpace.y < 0)
		return float4(.5, .5, .5, 0);
	if (voxSpace.z > 1 || voxSpace.z < 0)
		return float4(.5, .5, .5, 0);

#ifdef CELL_INTERPOLATION
	// I did this as an experiment, the results are a little bit smoother for values between one cell and another.
	// Basically the same thing linear filter does, but we can't set it from texture flags, since it linear filtering
	// will not work properly with the voxel update shader.

	// I don't think it's necessary, since results are very similair, but it requires 8 texture samples and a lot more calculations.
	// Will leave this here anyway in case someone wants to use it for some reason.

	float4 sam = .5;
	float3 textureSpace = voxSpace * _volumeMapTexSize.xyz;

	float4 texelSize = 1 / _volumeMapTexSize;
	texelSize.w = 0;

	float3 lowerCell = (round(textureSpace)-1) * texelSize;

	float3 diff = frac(textureSpace);

	// Remember texelSize.w is set to 0, so for example texelSize.wwz = float3(0, 0, texelSize.z)

	
	float4 smp000 = tex3Dlod(_volumeMap, float4(lowerCell, 0));
	float4 smp001 = tex3Dlod(_volumeMap, float4(lowerCell + texelSize.wwz, 0));
	float4 smp010 = tex3Dlod(_volumeMap, float4(lowerCell + texelSize.wyw, 0));
	float4 smp011 = tex3Dlod(_volumeMap, float4(lowerCell + texelSize.wyz, 0));
	float4 smp100 = tex3Dlod(_volumeMap, float4(lowerCell + texelSize.xww, 0));
	float4 smp101 = tex3Dlod(_volumeMap, float4(lowerCell + texelSize.xwz, 0));
	float4 smp110 = tex3Dlod(_volumeMap, float4(lowerCell + texelSize.xyw, 0));
	float4 smp111 = tex3Dlod(_volumeMap, float4(lowerCell + texelSize.xyz, 0));

	float4 p00 = smp000 + (smp001 - smp000) * diff.z;
	float4 p01 = smp010 + (smp011 - smp010) * diff.z;
	float4 p10 = smp100 + (smp101 - smp100) * diff.z;
	float4 p11 = smp110 + (smp111 - smp110) * diff.z;

	float4 c1 = p00 + (p01 - p00) * diff.y;
	float4 c2 = p10 + (p11 - p10) * diff.y;

	sam = c1 + (c2 - c1) * diff.x;


#else
	float4 sam = tex3Dlod(_volumeMap, float4(voxSpace, 0));
#endif
	
	return sam;
}


// Number of grass benders that the shader will process
int _grassBendersCount;

// Currently using custom StructuredBuffers in surface shaders is not fully supported
// so we have to use multiple arrays

// ##################################

float4x4 _benderWorld2Local[MAX_BENDERS];		// World to local space transformation matrix array
float4x4 _benderLocal2World[MAX_BENDERS];		// Local to world space transformation matrix array
float4 _benderHPDN[MAX_BENDERS];				// (Hardness, Power, Directionality, Noise) vector array

// ##################################


// Calculate the influence of a specific grass bender (index) on a world point.
float3 CalcBenderVolume(float3 worldPos, int index, inout float atten)
{

	float3 diff = mul(_benderWorld2Local[index], float4(worldPos, 1)).xyz;

	// if any of our transformed coord is out of -1,1 range, we already know it wont be affected by this volume
	if (abs(diff.x) > 1)
		return 0;
	if (abs(diff.y) > 1)
		return 0;
	if (abs(diff.z) > 1)
		return 0;

	float3 dir = lerp(normalize(diff), float3(0, 0, 1), _benderHPDN[index].z);
	float noise = 1 + ((cos(_Time.y * 3.0 + cos(dot(dir, diff * _benderHPDN[index].w*2))) * .5 + .5) * _benderHPDN[index].w);
	diff = pow(abs(diff+dir/2)/2, _benderHPDN[index].x);
	float mult = saturate(1 - sqrt(dot(diff, diff)));
		
	mult = _benderHPDN[index].y * mult;
	atten = mult;
	dir = normalize(mul((float3x3)_benderLocal2World[index], dir));

	// Comment this line if you want omnidirectional benders to push the grass both up and down along the y axis
	dir.y = lerp(-abs(dir.y), dir.y, _benderHPDN[index].z);
	
	return dir * mult * noise *  _benderHPDN[index].y;
}

// Calculate the influence of all the benders on a given world point.
float3 ApplyBending(float3 worldPos, float factor, inout float influence)
{
	if (factor < .05)
		return 0;

	float3 totalDir = 0;
	float tot = 1;
	for (int i = 0; i < _grassBendersCount; i++)
	{
		if (_benderHPDN[i].y > .1)
		{
			float a = 0;
			totalDir += CalcBenderVolume(worldPos, i, a);
			tot += a;
			if (a > influence)
				influence = a;
		}
	}
	return (totalDir/tot) * factor;
	
}


// Automatically move the vertex passed as parameter.
void BendVertex(inout float4 vertex, inout fixed factor)
{
	float3 worldPos = mul(unity_ObjectToWorld, vertex).xyz;

	float4 bendDir = 0;
#ifdef GRASS_PERMANENT_BEND
	bendDir = SampleVoxel(worldPos);
	bendDir.rgb = bendDir.rgb * 2 - 1;
	bendDir.rgb *= factor * _volumeBendMult;
#endif

#ifndef DISABLE_GRASS_VERTEX_BEND
	float i;
	bendDir.rgb += (1 - bendDir.a) * ApplyBending(worldPos, factor, i);
#endif
	vertex.xyz += mul((float3x3)unity_WorldToObject, float4(bendDir.rgb, 0.0));
	
}

// Automatically move the vertex passed as parameter.
void BendVertex(inout float4 vertex, inout fixed factor, inout fixed permanentValue)
{
	float3 worldPos = mul(unity_ObjectToWorld, vertex).xyz;

	float4 bendDir = 0;
#ifdef GRASS_PERMANENT_BEND
	bendDir = SampleVoxel(worldPos);
	bendDir.rgb = bendDir.rgb * 2 - 1;
	permanentValue = max(0, dot(bendDir.rgb, float3(0, -1, 0)));
	bendDir.rgb *= factor * _volumeBendMult;
	
#endif

#ifndef DISABLE_GRASS_VERTEX_BEND
	float i;
	bendDir.rgb += (1 - bendDir.a) * ApplyBending(worldPos, factor, i);
#endif
	vertex.xyz += mul((float3x3)unity_WorldToObject, float4(bendDir.rgb, 0.0));

}

// used in voxel calculations
float CalculateInfluence(float3 worldPos)
{
	float influence = 0;
	float3 totalDir = 0;
	for (int i = 0; i < _grassBendersCount; i++)
	{
		if (_benderHPDN[i].y > .1)
		{
			float a = 0;
			totalDir += CalcBenderVolume(worldPos, i, a);
			if (a > influence)
				influence = a;
		}
	}
	return influence;
}

#endif