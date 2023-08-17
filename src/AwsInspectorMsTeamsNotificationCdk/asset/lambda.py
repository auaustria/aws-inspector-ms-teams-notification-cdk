import json
import os
import logging
import urllib.request
import boto3
from urllib.error import URLError, HTTPError

logger = logging.getLogger()
logger.setLevel(logging.INFO)

def getWebhookUri():
    secret_name = "aws-inspector-ms-teams-webhook"
    region_name = os.environ['AWS_REGION']

    session = boto3.session.Session()
    client = session.client(
        service_name='secretsmanager',
        region_name=region_name
    )

    get_secret_value_response = client.get_secret_value(
        SecretId=secret_name
    )
    secret_value = get_secret_value_response['SecretString']
    secret_dict = json.loads(secret_value)
    return secret_dict['uri']

webhookUri = getWebhookUri()

def getMessageColor(severity):
    switcher = {
        "INFORMATIONAL": "good",
        "LOW": "good",
        "MEDIUM": "warning",
        "HIGH": "attention",
        "CRITICAL": "attention",
    }
    return switcher.get(severity, "default")

def handler(event, context):   
    try:
        factset = [
            {
                "title": "Account",
                "value": f"{event['account']}"
            },
            {
                "title": "Region",
                "value": f"{event['region']}"
            },
            {
                "title": "AWS Inspector Finding ARN",
                "value": f"{event['detail']['findingArn']}"
            },
            {
                "title": "Description",
                "value": f"{event['detail']['description']}"
            },
            {
                "title": "Score",
                "value": f"{event['detail']['inspectorScore']}"
            },
            {
                "title": "First Observed At",
                "value": f"{event['detail']['firstObservedAt']}"
            },
            {
                "title": "Last Observed At",
                "value": f"{event['detail']['lastObservedAt']}"
            },
            {
                "title": "Inspector Findings Raw Data",
                "value": f"{event}"
            }
        ]

        headerText = f"AWS INSPECTOR FINDING: {event['detail']['severity']}"
               
        msg = {  
                   "type":"message",  
                   "attachments":[  
                      {  
                         "contentType":"application/vnd.microsoft.card.adaptive",  
                         "content":{  
                            "$schema":"http://adaptivecards.io/schemas/adaptive-card.json",  
                            "type":"AdaptiveCard",  
                            "version":"1.4",  
                            "body": [
                        {
                            "type": "TextBlock",
                            "size": "Medium",
                            "weight": "Bolder",
                            "text":  headerText,
                            "color": f"{getMessageColor(event['detail']['severity'])}"
                        },
                        {
                            "type": "FactSet",
                            "facts": factset
                        }
                    ],
                    "actions": [
                            {
                                "type": "Action.OpenUrl",
                                "title": "View Details (use Finding ARN as filter)",
                                "url": f"https://{event['region']}.console.aws.amazon.com/inspector/v2/home?region={event['region']}#/findings"
                            }
                    ]
                    }  
                    }  
                ]  
            }
        
        jsonData = json.dumps(msg).encode('utf-8')
        logger.info(f"Request Message: {jsonData}")

        response = urllib.request.urlopen(urllib.request.Request(webhookUri, data=jsonData, headers={"Content-Type": "application/json"}))
        response.read()

    except HTTPError as err:
        logger.info(err)
        logger.error(f"Request failed: {err.code} {err.reason} {err.read().decode('utf-8')}")
    except URLError as err:
        logger.error(f"Server connection failed: {err.reason}")
