![Gitlab pipeline status](https://img.shields.io/gitlab/pipeline/ionburst/ionburst-sdk-net/main?color=fb6a26&style=flat-square)
[![slack](https://img.shields.io/badge/Slack-4A154B?style=flat-square&logo=slack&logoColor=white)](https://join.slack.com/t/ionburst-cloud/shared_invite/zt-panjkslf-Z5DOpU1OOeNPkXgklD~Cpg)

# Ionburst Cloud IonFS

**IonFS CLI** has been developed to illustrate how a client application typically integrates with Ionburst Cloud. 

IonFS provides a set of tools to manage data and secrets stored by Ionburst Cloud as if it were a remote filesystem. While IonFS stores primary data items within Ionburst Cloud, the metadata is stored in an S3 bucket. Anyone that has been granted access to this repository, and the appropriate Ionburst Cloud credentials, can interact with the stored data.
A getting started tutorial for IonFS is available on the Ionburst Cloud website [here.](https://ionburst.cloud/tutorials/get-started-with-ionfs) 

## Overview

```
# ionfs --help 

IonFS: 
  Securing your data on Ionburst Cloud. 

Usage: 
  IonFS [options] [command] 

Options: 
  -v, --version 
  -?, -h, --help    Show help and usage information 

Commands: 
  list <folder>               show the contents of a remote path, prefix remote paths with ion:// 
  get <from>                  download a file, prefix remote paths with ion:// 
  put <localfile> <folder>    upload a file, prefix remote paths with ion:// 
  del <path>                  delete an object, prefix remote paths with ion:// 
  move <from> <to>            move a file to/from a remote file system, prefix remote paths with ion:// 
  copy <from> <to>            copy a file to/from a remote file system, prefix remote paths with ion:// 
  mkdir <folder>              create a folder, prefix remote paths with ion:// 
  rmdir <folder>              remove a folder, prefix remote paths with ion:// 
  policy                      list the current Ionburst Cloud Classification Policies 
  repos                       list the currently registered Repositories 
```

## Configuration

The primary configuration for IonFS is managed within the `appsettings.json` file, located in the `.ionfs` folder in your home directory.

The IonFS section contains the main configuration items: 

```
  "IonFS": {
    "MaxSize": "65536",
    "Verbose": "false",
    "DefaultClassification": "Restricted",
    "Repositories": [ 
      { 
        "Name": "first-S3", 
        "Class": "Ionburst.Apps.IonFS.MetadataS3", 
        "DataStore": "ionfs-metadata-first" 
      }, 
      { 
        "Name": "second-S3", 
        "Class": "Ionburst.Apps.IonFS.MetadataS3", 
        "DataStore": "ionfs-metadata-second" 
      } 
    ],
    "DefaultRepository": "first-S3", 
  },
  "Ionburst": { 
    "Profile": "example_profile", 
    "IonburstUri": "https://api.example.ionburst.cloud/", 
    "TraceCredentialsFile": "OFF" 
  },
  "AWS": { 
    "Profile": "example", 
    "Region": "eu-west-1" 
  }
```

- `MaxSize` controls the chunking of data items being uploaded
- `Verbose` can be set to true to log extra details - Note, some commands allow this to be overridden on the command line using `-v` or `--version`
- `DefaultClassification` is the default Ionburst Cloud policy applied to data being uploaded, this can be explicitly set on the `put` command
- The `Repositories` section allows multiple metadata repositories to be accessed.   
- If a repository is not explicitly included in the remote path, the `DefaultRepository` will be used. 
- The `Ionburst` section is required to configure the Ionburst SDK. 
- The `AWS` section is required to access the AWS SDK. 

## Getting Help

Please use the following community resources to get help. We use [Gitlab issues][sdk-issues] to track bugs and feature requests.
- Join the Ionburst Cloud community on [Slack](https://join.slack.com/t/ionburst-cloud/shared_invite/zt-panjkslf-Z5DOpU1OOeNPkXgklD~Cpg)
- Get in touch with [Ionburst Support](https://ionburst.cloud/contact/)
- If it turns out that you may have found a bug, please open an [issue][sdk-issues]

### Opening Issues

If you find a bug, or have an issue with IonFS, we would like to hear about it. Check the existing [issues][sdk-issues] and try to make sure your problem doesn’t already exist before opening a new issue. It’s helpful if you include the version of IonFS and the OS you’re using. Please include a stack trace and steps to reproduce the issue.

The [GitLab issues][sdk-issues] are intended for bug reports and feature requests. For help and questions on IonFS, please make use of the resources listed in the Getting Help section. There are limited resources available for handling issues and by keeping the list of open issues clean we can respond in a timely manner.

## SDK Changelog

The changelog for IonFS can be found in the [CHANGELOG file.](CHANGELOG.md)

## Versioning

IonFS uses Semantic Versioning, e.g. MAJOR.MINOR.PATCH.

[ionburst]: https://ionburst.io
[ionburst-cloud]: https://ionburst.cloud
[sdk-website]: https://ionburst.cloud/docs/sdk/
[sdk-source]: https://gitlab.com/ionburst/ionfs
[sdk-issues]: https://gitlab.com/ionburst/ionfs/issues
[sdk-license]: https://gitlab.com/ionburst/ionfst/-/blob/master/LICENSE
[docs-api]: https://ionburst.cloud/docs/api/
