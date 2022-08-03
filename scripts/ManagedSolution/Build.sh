#!/bin/bash
#az bicep build --file ./main.bicep --outfile app/mainTemplate.json

#zip -j app.zip app/createUiDefinition.json app/mainTemplate.json
echo "Running az cli $(az version | jq '."azure-cli"' ), should be 2.37.0 or higher"

basedir="$( dirname "$( readlink -f "$0" )" )"

CONFIG_FILE="${basedir}/../config.json"

if [ ! -f "$CONFIG_FILE" ]; then
    cp config-template.json "${CONFIG_FILE}"
    echo "You need to configure deployment settings in ${CONFIG_FILE}" 
    exit 1
fi

function get-value { 
    local key="$1" ;
    local json ;
    
    json="$( cat "${CONFIG_FILE}" )" ;
    echo "${json}" | jq -r "${key}"
}

function put-value { 
    local key="$1" ;
    local variableValue="$2" ;
    local json ;
    json="$( cat "${CONFIG_FILE}" )" ;
    echo "${json}" \
       | jq --arg x "${variableValue}" "${key}=(\$x)" \
       > "${CONFIG_FILE}"
}

function put-json-value { 
    local key="$1" ;
    local variableValue="$2" ;
    local json ;
    json="$( cat "${CONFIG_FILE}" )" ;
    echo "${json}" \
       | jq --arg x "${variableValue}" "${key}=(\$x | fromjson)" \
       > "${CONFIG_FILE}"
}

jsonpath=".initConfig.subscriptionId"
subscriptionId="$( get-value "${jsonpath}" )"
[ "${subscriptionId}" == "" ] && { echo "Please configure ${jsonpath} in file ${CONFIG_FILE}" ; exit 1 ; }

jsonpath=".initConfig.resourceGroupName"
resourceGroupName="$( get-value  "${jsonpath}" )"
[ "${resourceGroupName}" == "" ] && { echo "Please configure ${jsonpath} in file ${CONFIG_FILE}" ; exit 1 ; }

jsonpath=".initConfig.location"
location="$( get-value  "${jsonpath}" )"
[ "${location}" == "" ] && { echo "Please configure ${jsonpath} in file ${CONFIG_FILE}" ; exit 1 ; }

jsonpath=".initConfig.suffix"
suffix="$( get-value  "${jsonpath}" )"
[ "${suffix}" == "" ] && { echo "Please configure ${jsonpath} in file ${CONFIG_FILE}" ; exit 1 ; }

jsonpath=".initConfig.groupName"
groupName="$( get-value  "${jsonpath}" )"
[ "${groupName}" == "" ] && { echo "Please configure ${jsonpath} in file ${CONFIG_FILE}" ; exit 1 ; }


jsonpath=".managedApplication.partnerCenterTrackingId"
subscriptionId="$( get-value "${jsonpath}" )"
[ "${subscriptionId}" == "" ] && { echo "Please configure ${jsonpath} in file ${CONFIG_FILE}" ; exit 1 ; }



# echo "subscriptionId    ${subscriptionId}"
# echo "resourceGroupName ${resourceGroupName}"
# echo "location          ${location}"
# echo "suffix            ${suffix}"
# echo "groupName         ${groupName}"




# 
# Perform the ARM deployment
#
deploymentResultJSON="$( az  bicep build --file ./main.bicep --outfile app/mainTemplate.json )"

echo "${deploymentResultJSON}" > results.json

zip -j app.zip app/createUiDefinition.json app/mainTemplate.json

echo "Finished setup..."