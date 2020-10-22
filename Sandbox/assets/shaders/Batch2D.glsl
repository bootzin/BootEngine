#type vertex
#version 430 core

layout(set = 0, binding = 0) uniform ViewProjection
{
	mat4 view_projection_matrix;
};

layout(location = 0) in vec3 Position;
layout(location = 1) in vec2 TexCoord;
layout(location = 2) in vec4 Color;
layout(location = 3) in float TexIndex;
layout(location = 4) in float TilingFactor;

layout(location = 0) out vec2 fsTexCoord;
layout(location = 1) out vec4 fsColor;
layout(location = 2) out float fsTexIndex;
layout(location = 3) out float fsTilingFactor;

void main()
{
	fsTexCoord = TexCoord;
	fsColor = Color;
	fsTexIndex = TexIndex;
	fsTilingFactor = TilingFactor;
	gl_Position = view_projection_matrix * vec4(Position, 1.0);
}

#type fragment
#version 430 core

layout(location = 0) in vec2 fsTexCoord;
layout(location = 1) in vec4 fsColor;
layout(location = 2) in float fsTexIndex;
layout(location = 3) in float fsTilingFactor;

layout(location = 0) out vec4 outColor;

layout(set = 0, binding = 1) uniform texture2D Texture;
layout(set = 0, binding = 2) uniform sampler Sampler;

void main()
{
	outColor = texture(sampler2D(Texture, Sampler), fsTexCoord * fsTilingFactor) * fsColor;
}