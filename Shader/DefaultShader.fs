#version 330 core

//#extension GL_ARB_explicit_uniform_location : require

in vec3 fragPos;
in vec2 fragTexCoord;
in vec3 fragNormal;

in vec3 cameraPos;
in vec4 fragPosLightSpace;

out vec4 finalColor;

uniform sampler2D Texture;
uniform sampler2D depthMap;

struct Material
{
    vec3 ambiant;
    vec3 diffuse;
    vec3 specular;
	float transparency;
};

uniform Material materials;

struct Light
{
	vec4 pos;   // pos. w  = _diffuseCoefficient
	vec4 color; // color.w = _ambiantCoefficient 
	vec4 dir;   // dir.w   = _specularCoefficient
    vec4 ltype; // ltype.w = _enabled

    vec4 attenuation; // x = attenuation; y = constant; z = linear; w = exponential
    float coneAngle;
};

uniform float 	lightCount;
uniform int 	receiveShadow;

#define NB_LIGHTS 50
layout (std140) uniform Lights 
{
	Light lights[NB_LIGHTS];
};

float shininess;

float ApplyShadow(Light light)
{	
	vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
	
	projCoords = projCoords * 0.5  + 0.5;	
	
	float closestDepth = texture(depthMap, projCoords.xy).r;
	float currentDepth = projCoords.z;
	
	if (projCoords.z > 1.0)
		return 0.0;
	
	float bias = 0.005;
	//float bias = max(0.05 * (1.0 - dot(fragNormal, normalize(fragPos - light.pos.xyz))), 0.005);
	
	float shadow = 0.0;
	vec2 texelSize = 1.0 / textureSize(depthMap, 1);
	for (int x = -1; x < 1; ++x)
	{
		for (int y = -1; y < 1; ++y)
		{
			float pcfDepth = texture(depthMap, projCoords.xy + vec2(x, y) * texelSize).r;
			shadow += currentDepth - bias > pcfDepth ? 1.0 : 0.0;
		}
	}
	//shadow /= 9.0;
		
	return shadow;
}

vec4 DirectionalLight(Light dirLight, vec4 texColor)
{
    vec3 surfaceToLight = normalize(dirLight.pos.xyz - fragPos);  // no minus
    vec3 surfaceToCamera = normalize(cameraPos - fragPos);
	
    // ambiant
    vec3 ambiant = dirLight.color.w * materials.ambiant * texColor.rgb * dirLight.color.xyz; 

    // diffuse
    float diffuseCoefficient = max(dot(fragNormal, -dirLight.dir.xyz), 0.0);
	
    vec3 diffuse = dirLight.pos.w * diffuseCoefficient * materials.diffuse * texColor.rgb * dirLight.color.xyz; 

    // specular
    float specularCoefficient = 0.0;
    if (diffuseCoefficient > 0.0)
        specularCoefficient = pow(max(dot(surfaceToCamera, normalize(reflect(-surfaceToLight, fragNormal))), 0.0), shininess);

    vec3 specular = dirLight.dir.w * specularCoefficient * materials.specular * dirLight.color.xyz; 

    // linear color
	float shadow = 0.0;
	if (receiveShadow == 1.0)
		shadow = ApplyShadow(dirLight);
	
	vec3 linearColor = (ambiant + (1.0 - shadow) * (diffuse + specular));

    return vec4(linearColor, texColor.a * materials.transparency);
}

vec4 SpotLight(Light spotLight, vec4 texColor)
{
    float distanceAttenuation = 1.0; //
    float distanceToLight = length(spotLight.pos.xyz - fragPos);

    vec3 surfaceToLight = normalize(spotLight.pos.xyz - fragPos);

    vec3 rayDir = -surfaceToLight;

    // ambiant
    vec3 ambiant = spotLight.color.w * texColor.rgb * spotLight.color.rgb;

    // diffuse
    float lightToSurfaceAngle = degrees(acos(dot(rayDir, normalize(spotLight.dir.xyz))));

    float diffuseCoefficient = 0.0;
    if (lightToSurfaceAngle < spotLight.coneAngle)
    {
        diffuseCoefficient = max(0.0, dot(fragNormal, surfaceToLight));
        distanceAttenuation = clamp((spotLight.attenuation.x * 100.0)/ (pow(distanceToLight, 2.0)), 0.0, 1.0); //
    }
    else
        distanceAttenuation = 0.0; //

    vec3 diffuse = spotLight.pos.w * diffuseCoefficient * texColor.rgb * spotLight.color.rgb;

    // specular
    float specularCoefficient = 0.0;
    if (diffuseCoefficient > 0.0)
     {
       vec3 reflectionVec = reflect(rayDir, fragNormal);
       vec3 surfaceToCamera = normalize(cameraPos - fragPos);
       float cosAngle = max(0.0, dot(surfaceToCamera, reflectionVec));
       specularCoefficient = pow(cosAngle, shininess);
       }
       
    vec3 specular = spotLight.dir.w * specularCoefficient * spotLight.color.rgb;
        
    // linear Color
    float angleAttenuation = pow((spotLight.coneAngle - lightToSurfaceAngle) / spotLight.coneAngle, 2);
    float attenuation = angleAttenuation * distanceAttenuation;

	float shadow = 0.0;
	if (receiveShadow == 1.0)
		shadow = ApplyShadow(spotLight);
		
	vec3 linearColor = ambiant + attenuation * ((1.0 - shadow) * (diffuse + specular));

    return vec4(linearColor, texColor.a * materials.transparency);
}

vec4 PointLight(Light pointLight, vec4 texColor)
{
    vec3 surfaceToCam = normalize(cameraPos - fragPos);
    vec3 surfaceToLight = normalize(pointLight.pos.xyz - fragPos);

    vec3 ambiant = pointLight.color.w * texColor.rgb * pointLight.color.xyz;

    float diffuseCoef = max(dot(fragNormal, surfaceToLight), 0.0);
    vec3 diffuse = pointLight.pos.w * diffuseCoef * texColor.rgb * pointLight.color.xyz;

    float specularCoef = 0.0;
    if (diffuseCoef > 0.0)
    {
        vec3 reflectVec = normalize(reflect(pointLight.dir.xyz , fragNormal));
        specularCoef = pow(dot(surfaceToCam, reflectVec), 2);
    }
    else 
        return vec4(0.0, 0.0 ,0.0, 0.0);

    vec3 specular = pointLight.dir.w * specularCoef * texColor.rgb * pointLight.color.xyz;

    float distance = length(pointLight.pos.xyz - fragPos);

    float attenuation  = pointLight.attenuation.y + 
                        pointLight.attenuation.z * distance + 
                        pointLight.attenuation.w * pow(distance, 2);
					
						
    vec3 linearColor = ambiant + (diffuse + specular);
    linearColor /= attenuation;
    
    float distanceAttenuation = clamp((pointLight.attenuation.x * 100.0)/ (pow(distance, 2.0)), 0.0, 1.0); //
    linearColor *= distanceAttenuation;

    return vec4(linearColor, texColor.a * materials.transparency);
}

vec4 ApplyLight(Light toApply, vec4 texColor)
{
    if (toApply.ltype.x == 1.0)
        return DirectionalLight(toApply, texColor);
    else if (toApply.ltype.y == 1.0)
        return SpotLight(toApply, texColor);
    else if (toApply.ltype.z == 1.0)
        return PointLight(toApply, texColor);

    return vec4(0.0, 0.0, 0.0, 0.0);
}

void main(void)
{
	vec2 texCoords = vec2(fragTexCoord.x, 1.0 - fragTexCoord.y);
    vec4 texColor = texture(Texture, texCoords);

    shininess = 20.0;

    finalColor = vec4(0.0, 0.0, 0.0, 1.0); 
    for (int i = 0; i <  lightCount; ++i)
        if (lights[i].ltype.w == 1.0)   
            finalColor = ApplyLight(lights[i], texColor);
	
	//float depthValue = texture(depthMap, texCoords).r;
	//finalColor = vec4(vec3(depthValue), 1.0);
}