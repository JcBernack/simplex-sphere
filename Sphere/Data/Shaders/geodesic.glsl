-- Vertex
#version 400

in vec4 Position;
out vec3 vPosition;

uniform mat4 ModelMatrix;

void main()
{
    vPosition = (ModelMatrix * Position).xyz;
}

-- TessControl
#version 400

#define ID gl_InvocationID
layout(vertices = 3) out;
in vec3 vPosition[];
out vec3 tcPosition[];

uniform float TessellationScale;
uniform float EdgesPerScreenHeight;

uniform float ClipNear;
uniform float ClipFar;
uniform vec3 CameraPosition;

uniform mat4 ViewMatrix;
uniform mat4 ProjectionMatrix;

// source for tessellation level calculation:
// https://developer.nvidia.com/content/dynamic-hardware-tessellation-basics

float GetTessLevel(vec3 cp1, vec3 cp2)
{
	vec3 midpoint = (cp1 + cp2) / 2.0;
	float scale = 1 - ((distance(midpoint, CameraPosition) - ClipNear) / (ClipFar - ClipNear));
	return clamp(TessellationScale * scale * 63 + 1, 1, 64);
}

float GetTessLevel2(vec3 cp1, vec3 cp2)
{
	float D = distance(cp1, cp2);
	vec3 midpoint = (cp1 + cp2) / 2.0;
	vec4 clipPos = ProjectionMatrix * ViewMatrix * vec4(midpoint, 1);
	D = abs(D * ProjectionMatrix[1][1] / clipPos.w);
	return D * EdgesPerScreenHeight;
}

void main()
{
	// copy over control points
	tcPosition[ID] = vPosition[ID];
	// determine tesselation level
	//gl_TessLevelOuter[ID] = GetTessLevel(vPosition[(ID+1)%3], vPosition[(ID+2)%3]);
	if (ID == 0)
	{
		gl_TessLevelOuter[0] = GetTessLevel2(vPosition[1], vPosition[2]);
		gl_TessLevelOuter[1] = GetTessLevel2(vPosition[2], vPosition[0]);
		gl_TessLevelOuter[2] = GetTessLevel2(vPosition[0], vPosition[1]);
		gl_TessLevelInner[0] = max(max(gl_TessLevelOuter[0], gl_TessLevelOuter[1]), gl_TessLevelOuter[2]);
	}
}

-- TessEval
#version 400

layout(triangles, equal_spacing, cw) in;
in vec3 tcPosition[];

out vec3 tePosition;
out vec3 tePatchDistance;

uniform mat4 ModelMatrix;
uniform mat4 ViewMatrix;
uniform mat4 ProjectionMatrix;

float GetHeight(vec3 pos)
{
	float len = length(pos);
	float yaw = asin(pos.x / len);
	float pitch = asin(pos.y / len);
	return 10 * sin(pitch*10) * cos(yaw*10);
}

void main()
{
    vec3 p0 = gl_TessCoord.x * tcPosition[0];
    vec3 p1 = gl_TessCoord.y * tcPosition[1];
    vec3 p2 = gl_TessCoord.z * tcPosition[2];
    tePatchDistance = gl_TessCoord;
    tePosition = normalize(p0 + p1 + p2);
	tePosition *= length(tcPosition[0]) + GetHeight(tePosition);
    //tePosition = p0 + p1 + p2;
    gl_Position = ProjectionMatrix * ViewMatrix * vec4(tePosition, 1);
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

uniform mat3 NormalMatrix;

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
