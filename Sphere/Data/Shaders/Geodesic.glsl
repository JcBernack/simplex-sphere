-- Matrices
uniform mat4 ModelMatrix;
uniform mat4 ViewMatrix;
uniform mat4 ProjectionMatrix;
uniform mat4 ModelViewMatrix;
uniform mat4 ModelViewProjectionMatrix;
uniform mat3 NormalMatrix;

-- Terrain
#include Noise.3D

uniform float Radius;
uniform float TerrainScale;
uniform float HeightScale;

float GetTerrainHeight(vec3 unitPosition)
{
	return snoise(unitPosition * TerrainScale);
}

vec3 GetTerrainPosition(vec3 unitPosition)
{
	return unitPosition * (Radius + max(0, HeightScale * GetTerrainHeight(unitPosition)));	
}

-- Vertex
#version 400

#include Geodesic.Matrices
#include Geodesic.Terrain

in vec3 Position;

out VertexData
{
	vec3 unitPosition;
	vec3 terrainPosition;
} outs;

void main()
{
	// pass through the unit sphere vertex
	outs.unitPosition = Position;
	// calculate terrain vertex to improve tessellation level
	outs.terrainPosition = (ModelViewMatrix * vec4(GetTerrainPosition(Position), 1)).xyz;
}

-- TessControl
#version 400

layout(vertices = 3) out;

in VertexData
{
	vec3 unitPosition;
	vec3 terrainPosition;
} ins[];

out TessData
{
	vec3 unitPosition;
} outs[];

#include Geodesic.Matrices
#include Geodesic.Terrain
#define ID gl_InvocationID
uniform float EdgesPerScreenHeight;

// source for tessellation level calculation:
// https://developer.nvidia.com/content/dynamic-hardware-tessellation-basics
float GetTessLevel(int index1, int index2)
{
	vec3 p1 = ins[index1].terrainPosition;
	vec3 p2 = ins[index2].terrainPosition;
	vec4 clipPos = ProjectionMatrix * vec4(0.5 * (p1 + p2), 1);
	float D = abs(distance(p1, p2) * ProjectionMatrix[1][1] / clipPos.w);
	return clamp(D * EdgesPerScreenHeight, 1, 64);
}

void main()
{
	// copy over control points
	outs[ID].unitPosition = ins[ID].unitPosition;
	// determine tesselation level
	if (ID == 0)
	{
		gl_TessLevelOuter[0] = GetTessLevel(1, 2);
		gl_TessLevelOuter[1] = GetTessLevel(2, 0);
		gl_TessLevelOuter[2] = GetTessLevel(0, 1);
		gl_TessLevelInner[0] = max(max(gl_TessLevelOuter[0], gl_TessLevelOuter[1]), gl_TessLevelOuter[2]);
	}
}

-- TessEval.Odd
#version 400
layout(triangles, fractional_odd_spacing, cw) in;
#include Geodesic.TessEval.Main

-- TessEval.Even
#version 400
layout(triangles, fractional_even_spacing, cw) in;
#include Geodesic.TessEval.Main

-- TessEval.Equal
#version 400
layout(triangles, equal_spacing, cw) in;
#include Geodesic.TessEval.Main

-- TessEval.Main
in TessData
{
	vec3 unitPosition;
} ins[];

out FragmentData
{
	float height;
} outs;

#include Geodesic.Matrices
#include Geodesic.Terrain

void main()
{
	// calculate new point on the unit sphere
    vec3 p0 = gl_TessCoord.x * ins[0].unitPosition;
    vec3 p1 = gl_TessCoord.y * ins[1].unitPosition;
    vec3 p2 = gl_TessCoord.z * ins[2].unitPosition;
	vec3 p = normalize(p0 + p1 + p2);
	// calculate point on terrain
	outs.height = GetTerrainHeight(p);
	gl_Position = ModelViewProjectionMatrix * vec4(GetTerrainPosition(p), 1);
}

-- Fragment
#version 400

in FragmentData
{
	float height;
} ins;

out vec4 FragColor;

const int NumColors = 7;
const float Steps[] = float[NumColors](-1, -0.25, 0, 0.0625, 0.125, /*0.375,*/ 0.75, 0.9);
const vec3 Colors[] = vec3[NumColors](
	vec3(0, 0, 0.6), // deeps
	vec3(0, 0, 0.8), // shallow
	vec3(0, 0.5, 1), // shore
	vec3(0.9375, 0.9375, 0.25), // sand
	vec3(0.125, 0.625, 0), // grass
	//vec3(0.875, 0.875, 0), // dirt
	vec3(0.5, 0.5, 0.5), // rock
	vec3(1, 1, 1) //snow
);

void main()
{
	// mix colors for different heights
	float x = ins.height;
	vec3 color = mix(Colors[0], Colors[1], smoothstep(Steps[0], Steps[1], x));
	for (int i = 2; i < NumColors; i++)
	{
		color = mix(color, Colors[i], smoothstep(Steps[i-1], Steps[i], x));
	}
	// set fragment color
	FragColor = vec4(color, 1.0);
}
