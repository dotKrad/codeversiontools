import ruamel.yaml
import re
import subprocess
import os

# Define the YAML file path
yaml_file = "D:\\Work\\CDW\\Source\\microsoft-365\\deployment\\prod\\azure-pipelines\\prod-microsoft-365-api-pipeline.yml"
repo_path = "D:\\Work\\CDW\\Source\\microsoft-365"

os.chdir(repo_path)

# Ensure we are on the 'staging' branch
#subprocess.run(["git", "checkout", "staging"], check=True)


# Read the YAML file
# Load the YAML file while preserving order and formatting
yaml = ruamel.yaml.YAML()
yaml.preserve_quotes = True  # Keep the original quoting style

with open(yaml_file, "r") as file:
    data = yaml.load(file)

# Extract and increment the imageTag version
image_tag = data["variables"]["imageTag"]
match = re.match(r"(v\d+\.\d+\.)(\d+)", image_tag)

if match:
    prefix, version = match.groups()
    new_version = int(version) + 1
    new_image_tag = f"{prefix}{new_version}"
    data["variables"]["imageTag"] = new_image_tag
else:
    raise ValueError("Invalid imageTag format")

# Write the updated YAML file while preserving its structure
with open(yaml_file, "w") as file:
    yaml.dump(data, file)

# Git commit local changes
commit_message = f"{new_image_tag} Production"

subprocess.run(["git", "add", yaml_file], check=True)
subprocess.run(["git", "commit", "-m", commit_message], check=True)