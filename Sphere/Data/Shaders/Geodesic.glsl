-- Matrices
uniform mat4 ModelMatrix;
//uniform mat4 ViewMatrix;
uniform mat4 ProjectionMatrix;
uniform mat4 ModelViewMatrix;
uniform mat4 ModelViewProjectionMatrix;
uniform mat3 NormalMatrix;

-- Terrain
#include Noise.3D

uniform float Radius;
uniform float HeightScale;
uniform float TerrainScale;
uniform float Persistence;
uniform int Octaves = 6;

//TODO: maybe add back the height cutoff: max(0, HeightScale * height)
float GetTerrainInternal(float height)
{
	return Radius + HeightScale * height;
}

vec3 GetTerrainOctaves(vec3 unitPosition, out float height, out vec3 gradient)
{
	gradient = vec3(0);
	float frequency = 1;
	float amplitude = 1;
	float noise = 0;
	float rangeSum = 0;
	vec3 gradientLocal;
	for (int i = 0; i < Octaves; i++)
	{
		noise += snoise(unitPosition * TerrainScale * frequency, gradientLocal) * amplitude;
		gradient += gradientLocal * frequency * amplitude;
		rangeSum += amplitude;
		frequency *= 2;
		amplitude *= Persistence;
	}
	height = noise / rangeSum;
	float scale = GetTerrainInternal(height);
	gradient *= TerrainScale / scale / rangeSum;
	return unitPosition * scale;
}

float GetNoiseOctaves(vec3 position)
{
	float frequency = 1;
	float amplitude = 1;
	float noise = 0;
	float rangeSum = 0;
	for (int i = 0; i < Octaves; i++)
	{
		noise += snoise(position * frequency) * amplitude;
		rangeSum += amplitude;
		frequency *= 2;
		amplitude *= Persistence;
	}
	return noise / rangeSum;
}

vec3 GetTerrainOctaves(vec3 unitPosition)
{
	float height = GetNoiseOctaves(unitPosition * TerrainScale);
	return unitPosition * GetTerrainInternal(height);
}

vec3 GetTerrain(vec3 unitPosition, out float height, out vec3 gradient)
{
	height = snoise(unitPosition * TerrainScale, gradient);
	float scale = GetTerrainInternal(height);
	gradient *= TerrainScale / scale;
	return unitPosition * scale;
}

vec3 GetTerrain(vec3 unitPosition)
{
	return unitPosition * GetTerrainInternal(snoise(unitPosition * TerrainScale));
}

vec3 GetNormal(vec3 unitPosition, vec3 gradient)
{
	// gradient projected into the tangent-plane of the sphere in point p
	vec3 h = gradient - dot(gradient, unitPosition) * unitPosition;
	// normal on displaced sphere surface
	return normalize(unitPosition - HeightScale * h);
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
	outs.terrainPosition = (ModelViewMatrix * vec4(GetTerrainOctaves(Position), 1)).xyz;
}

-- TessControl
#version 400

layout(vertices = 3) out;

in VertexData
{
	vec3 unitPosition;
	vec3 terrainPosition;
} ins[];

out TessEvalData
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
	//TODO: patch-wise backface culling often produces wrong results, think of something better
	return true;
	vec4 edge1 = p1/p1.w-p0/p0.w;
	vec4 edge2 = p2/p2.w-p0/p0.w;
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
in TessEvalData
{
	vec3 unitPosition;
} ins[];

out TessControlData
{
	smooth vec3 unitPosition;
	smooth vec3 position;
	smooth vec3 patchCoord;
	smooth float height;
	smooth vec3 gradient;
} outs;

#include Geodesic.Matrices
#include Geodesic.Terrain

vec3 GetTessPos(vec3 coords)
{
	vec3 p0 = coords.x * ins[0].unitPosition;
    vec3 p1 = coords.y * ins[1].unitPosition;
    vec3 p2 = coords.z * ins[2].unitPosition;
	return normalize(p0 + p1 + p2);
}

void main()
{
	// output the barycentric coordinates to render the wireframe in the fragment shader
	outs.patchCoord = gl_TessCoord;
	// calculate new point on the unit sphere
	outs.unitPosition = GetTessPos(gl_TessCoord);
	// calculate point on terrain
	vec4 pos = vec4(GetTerrainOctaves(outs.unitPosition, outs.height, outs.gradient), 1);
	outs.position = (ModelMatrix * pos).xyz;
	gl_Position = ModelViewProjectionMatrix * pos;
}

-- Geometry
#version 400

layout(triangles) in;
layout(triangle_strip, max_vertices = 3) out;

in TessControlData
{
	smooth vec3 unitPosition;
	smooth vec3 position;
	smooth vec3 patchCoord;
	smooth float height;
	smooth vec3 gradient;
} ins[];

out GeometryData
{
	smooth vec3 unitPosition;
	smooth vec3 position;
	smooth vec3 patchCoord;
	smooth vec3 triangleCoord;
	smooth float height;
	smooth vec3 gradient;
} outs;

void Passthrough(int i, vec3 triangleCoord)
{
	outs.unitPosition = ins[i].unitPosition;
	outs.position = ins[i].position;
	outs.patchCoord = ins[i].patchCoord;
	outs.height = ins[i].height;
	outs.gradient = ins[i].gradient;
	outs.triangleCoord = triangleCoord;
	gl_Position = gl_in[i].gl_Position;
	EmitVertex();
}

void main()
{
	Passthrough(0, vec3(1,0,0));
	Passthrough(1, vec3(0,1,0));
	Passthrough(2, vec3(0,0,1));
	EndPrimitive();
}

-- Fragment
#version 400

in GeometryData
{
	smooth vec3 unitPosition;
	smooth vec3 position;
	smooth vec3 patchCoord;
	smooth vec3 triangleCoord;
	smooth float height;
	smooth vec3 gradient;
} ins;

layout (location = 0) out vec4 Position;
layout (location = 1) out vec4 Normal;
layout (location = 2) out vec4 Diffuse;
layout (location = 3) out vec4 Aux;

const int NumColors = 10;
const float Steps[] = float[NumColors](-1, -0.25, 0, 0.0625, 0.125, 0.2, 0.201, 0.5, 0.75, 0.9);
const vec3 Colors[] = vec3[NumColors](
	vec3(0, 0, 0.6), // deeps
	vec3(0, 0, 0.8), // shallow
	vec3(0, 0.5, 1), // shore
	vec3(0.9375, 0.9375, 0.25), // sand
	vec3(0.9375, 0.9375, 0.25), // sand
	vec3(0.125, 0.625, 0), // grass
	vec3(0.125, 0.625, 0), // grass
	vec3(0.47, 0.28, 0), // dirt
	vec3(0.5, 0.5, 0.5), // rock
	vec3(1, 1, 1) //snow
);

//const int NumColors = 4;
//const float Steps[] = float[NumColors](-1, -0.2, 0.2, 1);
//const vec3 Colors[] = vec3[NumColors](
//	vec3(0),
//	vec3(0.3),
//	vec3(0.7),
//	vec3(1)
//);

uniform bool EnableFragmentNormal;
uniform bool EnableNoiseTexture;
uniform bool EnableWireframe;

#include Geodesic.Matrices
#include Geodesic.Terrain

void main()
{
	vec3 position = ins.position;
	float height = ins.height;
	vec3 gradient = ins.gradient;
	// evaluate the noise again to calculate per-fragment normals
	if (EnableFragmentNormal)
	{
		position = GetTerrainOctaves(ins.unitPosition, height, gradient);
		position = (ModelMatrix * vec4(position, 1)).xyz;
	}
	// mix colors for different heights
	vec3 color = mix(Colors[0], Colors[1], smoothstep(Steps[0], Steps[1], height));
	for (int i = 2; i < NumColors; i++)
	{
		color = mix(color, Colors[i], smoothstep(Steps[i-1], Steps[i], height));
	}
	// add noise to color to give it some texture
	if (EnableNoiseTexture)
	{
		const float range = 0.1;
		color *= (1-range) + snoise(position*20) * range;
	}
	// add wireframe to color
	if (EnableWireframe)
	{
		float d = min(min(ins.patchCoord.x, ins.patchCoord.y), ins.patchCoord.z);
		color = mix(vec3(1,1,1), color, step(0.25, d*d*1000));
		d = min(min(ins.triangleCoord.x, ins.triangleCoord.y), ins.triangleCoord.z);
		color = mix(vec3(1,1,1), color, clamp(d*d*1000, 0, 1));
	}
	// output into gbuffer
	Position = vec4(position, 1);
	Normal = vec4(NormalMatrix * GetNormal(ins.unitPosition, gradient), 1);
	Diffuse = vec4(color, 1);
	Aux = vec4(normalize(abs(gradient)), 1);
}

-- Unused Stuff

// approximate normal with partial derivatives
vec3 X = dFdx(ins.position);
vec3 Y = dFdy(ins.position);
// output approximated error on the normals
Aux = vec4(Normal.xyz - normalize(cross(X,Y)), 1);

mat3 GetRotationMatrix(vec3 axis, float angle)
{
    axis = normalize(axis);
    float s = sin(angle);
    float c = cos(angle);
    float oc = 1.0 - c;
    return mat3(oc * axis.x * axis.x + c,           oc * axis.x * axis.y - axis.z * s,  oc * axis.z * axis.x + axis.y * s,
                oc * axis.x * axis.y + axis.z * s,  oc * axis.y * axis.y + c,           oc * axis.y * axis.z - axis.x * s,
                oc * axis.z * axis.x - axis.y * s,  oc * axis.y * axis.z + axis.x * s,  oc * axis.z * axis.z + c);
}
