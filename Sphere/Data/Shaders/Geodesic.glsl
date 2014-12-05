-- Matrices
uniform mat4 ModelMatrix;
uniform mat4 ViewMatrix;
uniform mat4 ProjectionMatrix;
uniform mat4 ModelViewMatrix;
uniform mat4 ModelViewProjectionMatrix;
uniform mat3 NormalMatrix;

-- Vertex
#version 400

in vec4 Position;
out vec3 vPosition;

#include Geodesic.Matrices

void main()
{
    vPosition = Position.xyz;
    //vPosition = (ModelViewMatrix * (Radius * Position)).xyz;
}

-- TessControl
#version 400

#define ID gl_InvocationID
layout(vertices = 3) out;
in vec3 vPosition[];
out vec3 tcPosition[];

#include Geodesic.Matrices

uniform float Radius;
uniform float EdgesPerScreenHeight;

// source for tessellation level calculation:
// https://developer.nvidia.com/content/dynamic-hardware-tessellation-basics
float GetTessLevel(vec3 cp1, vec3 cp2)
{
	vec3 midpoint = Radius * normalize((cp1 + cp2) / 2);
	vec4 p1 = ModelViewMatrix * vec4(Radius * cp1, 1);
	vec4 p2 = ModelViewMatrix * vec4(Radius * cp2, 1);
	vec4 clipPos = ModelViewProjectionMatrix * vec4(midpoint, 1);
	float D = abs(distance(p1, p2) * ProjectionMatrix[1][1] / clipPos.w);
	//vec3 midpoint = (cp1 + cp2) / 2;
	//vec4 clipPos = ProjectionMatrix * vec4(midpoint, 1);
	//float D = abs(distance(cp1, cp2) * ProjectionMatrix[1][1] / clipPos.w);
	return clamp(D * EdgesPerScreenHeight, 1, 64);
}

void main()
{
	// copy over control points
	tcPosition[ID] = vPosition[ID];
	// determine tesselation level
	//gl_TessLevelOuter[ID] = GetTessLevel(vPosition[(ID+1)%3], vPosition[(ID+2)%3]);
	if (ID == 0)
	{
		gl_TessLevelOuter[0] = GetTessLevel(vPosition[1], vPosition[2]);
		gl_TessLevelOuter[1] = GetTessLevel(vPosition[2], vPosition[0]);
		gl_TessLevelOuter[2] = GetTessLevel(vPosition[0], vPosition[1]);
		gl_TessLevelInner[0] = max(max(gl_TessLevelOuter[0], gl_TessLevelOuter[1]), gl_TessLevelOuter[2]);
	}
}

-- TessEval
#version 400

layout(triangles, equal_spacing, cw) in;
in vec3 tcPosition[];

out vec3 tePosition;
out vec3 tePatchDistance;

#include Geodesic.Matrices

uniform float Radius;
uniform float TerrainScale;

float GetHeight(vec3 pos)
{
	float len = length(pos);
	float yaw = asin(pos.x / len);
	float pitch = asin(pos.y / len);
	return sin(pitch*10) * cos(yaw*10);
}

void main()
{
    vec3 p0 = gl_TessCoord.x * tcPosition[0];
    vec3 p1 = gl_TessCoord.y * tcPosition[1];
    vec3 p2 = gl_TessCoord.z * tcPosition[2];
    tePatchDistance = gl_TessCoord;
    tePosition = normalize(p0 + p1 + p2);
	tePosition *= Radius + TerrainScale * GetHeight(tePosition);
    //tePosition = p0 + p1 + p2;
    //gl_Position = ProjectionMatrix * ViewMatrix * vec4(tePosition, 1);
    gl_Position = ModelViewProjectionMatrix * vec4(tePosition, 1);
}

-- Geometry
#version 400

layout(triangles) in;
layout(triangle_strip, max_vertices = 3) out;

in vec3 tePosition[3];
in vec3 tePatchDistance[3];

out vec3 gFacetNormal;
out vec3 gPatchDistance;
out vec3 gTriDistance;

#include Geodesic.Matrices

void main()
{
    vec3 A = tePosition[2] - tePosition[0];
    vec3 B = tePosition[1] - tePosition[0];
    gFacetNormal = NormalMatrix * normalize(cross(A, B));
    
    gPatchDistance = tePatchDistance[0];
    gTriDistance = vec3(1, 0, 0);
    gl_Position = gl_in[0].gl_Position;
	EmitVertex();

    gPatchDistance = tePatchDistance[1];
    gTriDistance = vec3(0, 1, 0);
    gl_Position = gl_in[1].gl_Position;
	EmitVertex();

    gPatchDistance = tePatchDistance[2];
    gTriDistance = vec3(0, 0, 1);
    gl_Position = gl_in[2].gl_Position;
	EmitVertex();

    EndPrimitive();
}

-- Fragment
#version 400

in vec3 gFacetNormal;
in vec3 gTriDistance;
in vec3 gPatchDistance;
in float gPrimitive;

out vec4 FragColor;

uniform vec3 LightPosition;
uniform vec3 DiffuseMaterial;
uniform vec3 AmbientMaterial;

float amplify(float d, float scale, float offset)
{
    d = scale * d + offset;
    d = clamp(d, 0, 1);
    d = 1 - exp2(-2*d*d);
    return d;
}

void main()
{
    vec3 N = normalize(gFacetNormal);
    vec3 L = LightPosition;
    float df = abs(dot(N, L));
    vec3 color = AmbientMaterial + df * DiffuseMaterial;

    float d1 = min(min(gTriDistance.x, gTriDistance.y), gTriDistance.z);
    float d2 = min(min(gPatchDistance.x, gPatchDistance.y), gPatchDistance.z);
    color = amplify(d1, 40, -0.5) * amplify(d2, 200, -0.5) * color;

    FragColor = vec4(color, 1.0);
}
