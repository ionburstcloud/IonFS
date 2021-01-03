# Ionburst Cloud IonFS [![Gitter](https://badges.gitter.im/ionburstlimited/community.svg)](https://gitter.im/ionburstlimited/community?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

**IonFS CLI** has been developed to illustrate how a client application typically integrates with Ionburst Cloud. 

IonFS provides a set of tools to manage data stored by Ionburst Cloud as if it were a remote filesystem. While IonFS stores primary data items within Ionburst Cloud, the metadata is stored in an S3 bucket; anyone with access to this bucket, and the appropriate Ionburst Cloud credentials, can interact with the stored data. 

An introduction to IonFS is available on the Ionburst Cloud developer blog [here.](https://ionburst.cloud/blog/ionfs-a-client-and-metadata-store-for-ionburst/) 

## Overview

```
# ionfs --help 

IonFS: 
  Securing your data on Ionburst. 

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
  policy                      list the current Ionburst Classification Policies 
  repos                       list the currently registered Repositories 
```

## Configuration

The primary configuration for IonFS is managed within appsettings.json, located in the .ionfs folder in your home directory. 

The IonFS section contains the main configuration items: 

```
  "IonFS": { 
```

MaxSize controls the chunking of data items being uploaded, for extra details to be logged to screen Verbose can be set to true.  Note, some commands allow this to be overridden on the command line using -v or --version.  DefaultClassification is the default Ionburst policy applied to data being uploaded, this can be explicitly set on the PUT command. 

```
    "MaxSize": "65536",
    "Verbose": "false",
    "DefaultClassification": "Restricted", 
```

The Repositories section allows multiple metadata repositories to be accessed.   

```
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
```

If a repository is not explicitly included in the remote path, the DefaultRepository will be used. 

```
    "DefaultRepository": "first-S3", 
  }, 
```

The Ionburst section is required to access the Ionburst SDK. 

```
  "Ionburst": { 
    "Profile": "example_profile", 
    "IonburstUri": "https://api.example.ionburst.io/", 
    "TraceCredentialsFile": "OFF" 
  },  

The AWS section is required to access the AWS SDK. 

  "AWS": { 
    "Profile": "example", 
    "Region": "eu-west-1" 
  }
```

## Getting Help

Please use the following community resources to get help. We use [Gitlab issues][sdk-issues] to track bugs and feature requests.
* Join the Ionburst chat on [gitter](https://gitter.im/ionburstlimited/community)
* Get in touch with [Ionburst Support](https://ionburst.cloud/contact/)
* If it turns out that you may have found a bug, please open an [issue][sdk-issues]

### Opening Issues

If you find a bug, or have an issue with the Ionburst SDK for .NET we would like to hear about it. Check the existing [issues][sdk-issues] and try to make sure your problem doesn’t already exist before opening a new issue. It’s helpful if you include the version of Ionburst SDK .NET and the OS you’re using. Please include a stack trace and reduced repro case when appropriate, too.

The [Gitlab issues][sdk-issues] are intended for bug reports and feature requests. For help and questions with using the Ionburst SDK for .NET please make use of the resources listed in the Getting Help section. There are limited resources available for handling issues and by keeping the list of open issues clean we can respond in a timely manner.

[ionburst]: https://ionburst.io
[ionburst-cloud]: https://ionburst.cloud
[sdk-website]: https://ionburst.cloud/docs/sdk/
[sdk-source]: https://gitlab.com/ionburst/ionfs
[sdk-issues]: https://gitlab.com/ionburst/ionfs/issues
[sdk-license]: https://gitlab.com/ionburst/ionfst/-/blob/master/LICENSE
[docs-api]: https://ionburst.cloud/docs/api/
