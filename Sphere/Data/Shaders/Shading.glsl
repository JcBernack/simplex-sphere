-- Vertex
#version 400

in vec3 Position;

uniform mat4 ModelViewProjectionMatrix;

void main()
{
	gl_Position = ModelViewProjectionMatrix * vec4(Position, 1.0);
}

-- Fragment.Light
#version 400

out vec4 FragColor;

/*
struct MaterialObject
{
	uint Type;
	vec3 Argument1;
	vec3 Argument2;
};
#define MAX_MATERIALS_OBJECTS 2
uniform int MaterialCount;
layout(std140) uniform MaterialBuffer
{
	MaterialObject Material[MAX_MATERIALS_OBJECTS];
};
*/
//TODO: add uniform buffer with light information
//TODO: generalize and support multiple materials
uniform float MaterialSpecularIntensity = 0.0;
uniform float MaterialSpecularPower = 0.0;

uniform vec3 LightColor = vec3(1);
uniform float AmbientIntensity = 0.15;
uniform float DiffuseIntensity = 0.75;

uniform vec3 EyePosition;

uniform sampler2D GPosition;
uniform sampler2D GNormal;
uniform sampler2D GDiffuse;
//uniform sampler2D GTexCoord;

uniform vec2 InverseScreenSize;

vec4 GetLightColor(vec3 position, vec3 normal, vec3 direction)
{
	float diffuse = 0;
	float specular = 0;
	float lightAngle = dot(normal, -direction);
	if (lightAngle > 0.0)
	{
		// calculate diffuse light intensity
		diffuse = DiffuseIntensity * lightAngle;
		// calculate specular light intensity
		vec3 eyeDirection = normalize(EyePosition - position);
		vec3 reflectedDirection = normalize(reflect(direction, normal));
		float eyeAngle = dot(eyeDirection, reflectedDirection);
		eyeAngle = pow(eyeAngle, MaterialSpecularPower);
		if (eyeAngle > 0.0)
		{
			specular = MaterialSpecularIntensity * eyeAngle;
		}
	}
	return (AmbientIntensity + diffuse + specular) * vec4(LightColor, 1.0);
}

// forward declaration
vec4 ProcessLight(vec3 position, vec3 normal);

void main()
{
	vec2 coord = gl_FragCoord.xy * InverseScreenSize;
	vec3 position = texture(GPosition, coord).xyz;
	vec3 normal = normalize(texture(GNormal, coord).xyz);
	vec3 color = texture(GDiffuse, coord).xyz;
	FragColor = vec4(color, 1.0) * ProcessLight(position, normal);
}

-- Fragment.Light.Directional
#include Shading.Fragment.Light

uniform vec3 LightDirection;

vec4 ProcessLight(vec3 position, vec3 normal)
{
	return GetLightColor(position, normal, LightDirection);
}

-- Fragment.Light.Point
#include Shading.Fragment.Light

uniform vec3 LightPosition;
uniform vec3 Attenuation;

vec4 ProcessLight(vec3 position, vec3 normal)
{
	// get light direction and distance
    vec3 direction = position - LightPosition;
    float d = length(direction);
	// get light color and factor in attenuation
    vec4 color = GetLightColor(position, normal, normalize(direction));
	float falloff = max(1.0, dot(Attenuation, vec3(1, d, d*d)));
    return color / falloff;
}