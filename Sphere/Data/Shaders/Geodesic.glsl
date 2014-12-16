-- Matrices
uniform mat4 ModelMatrix;
//uniform mat4 ViewMatrix;
uniform mat4 ProjectionMatrix;
uniform mat4 ModelViewMatrix;
uniform mat4 ModelViewProjectionMatrix;
//uniform mat3 NormalMatrix;

-- Terrain
#include Noise.3D

uniform float Radius;
uniform float TerrainScale;
uniform float HeightScale;

const int NumOctaves = 4;
const vec2 Octaves[] = vec2[NumOctaves](
	vec2(1, 1),
	vec2(2.312, 0.5),
	vec2(5.741, 0.25),
	vec2(12.384, 0.125)
);

float GetTerrainHeight(vec3 unitPosition)
{
	//return snoise(unitPosition * TerrainScale);
	vec3 p = unitPosition * TerrainScale;
	float n = 0;
	float range = 0;
	for (int i = 0; i < NumOctaves; i++)
	{
		n += snoise(p * Octaves[i].x) * Octaves[i].y;
		range += Octaves[i].y;
	}
	//float n = snoise(p) + snoise(p * 2.312) * 0.5 + snoise(p * 5.74) * 0.25;
	return n / range;
}

float GetTerrainDisplacement(float height)
{
	return Radius + max(0, HeightScale * height);
}

vec3 GetTerrainPosition(vec3 unitPosition)
{
	return unitPosition * GetTerrainDisplacement(GetTerrainHeight(unitPosition));
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

// source for tessellation level calculation: Nvidia
// https://developer.nvidia.com/content/dynamic-hardware-tessellation-basics
float GetTessLevel(int index1, int index2)
{
	vec3 p1 = ins[index1].terrainPosition;
	vec3 p2 = ins[index2].terrainPosition;
	vec4 clipPos = ProjectionMatrix * vec4(0.5 * (p1 + p2), 1);
	float D = abs(distance(p1, p2) * ProjectionMatrix[1][1] / clipPos.w);
	return clamp(D * EdgesPerScreenHeight, 1, 64);
}
// Frustrum test based on OpenGL Insights:
// https://books.google.de/books?id=CCVenzOGjpcC&pg=PA150&lpg=PA150&dq=choose+tessellation+level+projected+sphere&source=bl&ots=pcp7Dmd0EL&sig=fqSwcXWM28qxhyEhGd03vG8qGFM&hl=de&sa=X&ei=wWiKVMyCCoXtUoKCg5gJ&ved=0CC8Q6AEwAQ#v=onepage&q=choose%20tessellation%20level%20projected%20sphere&f=false
bool EdgeInFrustrum(vec4 p, vec4 q)
{
	return !((p.x < -p.w && q.x < -q.w) || (p.x > p.w && q.x > q.w)
		  || (p.y < -p.w && q.y < -q.w) || (p.y > p.w && q.y > q.w)
		  || (p.z < -p.w && q.z < -q.w) || (p.z > p.w && q.z > q.w));
}

bool IsFrontFace(vec4 p0, vec4 p1, vec4 p2)
{
	vec4 edge1 = p1/p1.w-p0/p0.w;
	vec4 edge2 = p2/p2.w-p0/p0.w;
	//TODO: patch-wise backface culling often produces wrong results, think of something better
	return cross(edge1.xyz, edge2.xyz).z < 0;
}

void main()
{
	// copy over patch points
	outs[ID].unitPosition = ins[ID].unitPosition;
	// continue only with the first invocation per patch
	if (ID > 0) return;
	// determine tesselation level
	vec4 p0 = ProjectionMatrix * vec4(ins[0].terrainPosition, 1);
	vec4 p1 = ProjectionMatrix * vec4(ins[1].terrainPosition, 1);
	vec4 p2 = ProjectionMatrix * vec4(ins[2].terrainPosition, 1);
	if (IsFrontFace(p0, p1, p2) && (EdgeInFrustrum(p0, p1) || EdgeInFrustrum(p1, p2) || EdgeInFrustrum(p2, p0)))
	{
		gl_TessLevelOuter[0] = GetTessLevel(1, 2);
		gl_TessLevelOuter[1] = GetTessLevel(2, 0);
		gl_TessLevelOuter[2] = GetTessLevel(0, 1);
		gl_TessLevelInner[0] = max(max(gl_TessLevelOuter[0], gl_TessLevelOuter[1]), gl_TessLevelOuter[2]);
	}
	else
	{
		gl_TessLevelOuter[0] = 0;
		gl_TessLevelOuter[1] = 0;
		gl_TessLevelOuter[2] = 0;
		gl_TessLevelInner[0] = 0;
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
	smooth vec4 position;
	smooth float height;
	smooth vec3 tessCoord;
} outs;

#include Geodesic.Matrices
#include Geodesic.Terrain

void main()
{
	// calculate new point on the unit sphere
	outs.tessCoord = gl_TessCoord;
    vec3 p0 = gl_TessCoord.x * ins[0].unitPosition;
    vec3 p1 = gl_TessCoord.y * ins[1].unitPosition;
    vec3 p2 = gl_TessCoord.z * ins[2].unitPosition;
	vec3 p = normalize(p0 + p1 + p2);
	// calculate point on terrain
	outs.height = GetTerrainHeight(p);
	vec4 pos = vec4(p * GetTerrainDisplacement(outs.height), 1);
	outs.position = ModelMatrix * pos;
	gl_Position = ModelViewProjectionMatrix * pos;
}

-- Fragment
#version 400

in FragmentData
{
	smooth vec4 position;
	smooth float height;
	smooth vec3 tessCoord;
} ins;

layout (location = 0) out vec4 Position;
layout (location = 1) out vec4 Diffuse;
layout (location = 2) out vec4 Normal;
layout (location = 3) out vec4 TexCoord;

const int NumColors = 8;
const float Steps[] = float[NumColors](-1, -0.25, 0, 0.0625, 0.125, 0.5, 0.75, 0.9);
const vec3 Colors[] = vec3[NumColors](
	vec3(0, 0, 0.6), // deeps
	vec3(0, 0, 0.8), // shallow
	vec3(0, 0.5, 1), // shore
	vec3(0.9375, 0.9375, 0.25), // sand
	vec3(0.125, 0.625, 0), // grass
	vec3(0.47, 0.28, 0), // dirt
	vec3(0.5, 0.5, 0.5), // rock
	vec3(1, 1, 1) //snow
);

uniform bool EnableWireframe;

void main()
{
	// mix colors for different heights
	//TODO: move color calculation to deferred shading pass
	float x = ins.height;
	vec3 color = mix(Colors[0], Colors[1], smoothstep(Steps[0], Steps[1], x));
	for (int i = 2; i < NumColors; i++)
	{
		color = mix(color, Colors[i], smoothstep(Steps[i-1], Steps[i], x));
	}
	// add wireframe to color
	if (EnableWireframe)
	{
		float d = min(min(ins.tessCoord.x, ins.tessCoord.y), ins.tessCoord.z);
		color *= clamp(d*d*1000, 0, 1);
	}
	// approximate normal
	vec3 X = dFdx(ins.position.xyz);
	vec3 Y = dFdy(ins.position.xyz);
	Normal = vec4(normalize(cross(X,Y)), 1);
	// output into gbuffer
	Position = ins.position;
	Diffuse = vec4(color, 1.0);
	//Normal = vec4(0);
	TexCoord = vec4((x+1)/2);
}
