#type vertex
#version 430 core

layout(set = 0, binding = 0) uniform ViewProjection
{
	mat4 view_projection_matrix;
};

layout(set = 0, binding = 1) uniform Transform
{
	mat4 model_matrix;
};

layout(location = 0) in vec3 Position;
layout(location = 1) in vec2 TexCoord;

layout(location = 0) out vec2 fsTexCoord;

void main()
{
	fsTexCoord = TexCoord;
	gl_Position = view_projection_matrix * model_matrix * vec4(Position, 1.0);
}

#type fragment
#version 430 core

layout(location = 0) in vec2 fsTexCoord;
layout(location = 0) out vec4 outColor;

layout(set = 0, binding = 2) uniform Color
{
	vec4 fsin_Color;
};

layout(set = 0, binding = 3) uniform texture2D Texture;
layout(set = 0, binding = 4) uniform sampler Sampler;
layout(set = 0, binding = 5) uniform TilingFactor 
{
	float tilingFactor;
};

void main()
{
	outColor = texture(sampler2D(Texture, Sampler), fsTexCoord * tilingFactor) * fsin_Color;
}