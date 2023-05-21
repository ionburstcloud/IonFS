# CHANGELOG

<!--- next entry here -->

## 0.4.0-develop.1
2023-05-21

### Features

- add tags to PUTs (38aba8b9d640ab56517be8bad16c5a88cee65c6e)
- first version of search command for LocalFS (b98e8204e17ee7f5c60d341bf6329465c25bb183)
- add regex for searching (7231d67a41a132fb28299d20e18569f7bd883b61)
- initial work to require per-repo Ionburst client (5fb67d438205c5b1b23e633344309decf5e047b6)
- add check commands (602d6456176603ecb48a4e5d6ea84aa02db7e8e8)

### Fixes

- Native by Default, don't need an option (7e863250be85acb5923ac4f3085939cbb0086922)
- ensure repo specific Ionburst client is used for operations (ee6fde66c958fea3ccdf49dfe4e413895078167e)
- added details to repos command to show URI, Status, and Profile, add rm/mv/cp aliases (3fc99f184a131683397fd96e72da7a976e90e4b0)
- set default config in S3 Wrapper to deal with a long delay on initialising S3, upgrade nuget packages (7de9b587b3dbc0daeee0bf4926d5ba27aabfb8cb)
- add concurrent dictionaries (413a97e3ef642ecc9d19572683d5a2c924acac1f)
- bug fix for del issue with async foreach, upgrade to net7.0, add check-id command (41e4da52829dfaa35ab9b5b47b721a08b7488173)
- console message improvements for del and check (04d1a41ed1e760402205db2b5ea7289969e425bc)
- clear up Put command verbose output (031b7f7c1d53e775541ed5b513d130087cc5c52b)
- improve console verbose output for GETs (a684d26ea8488e1dd9e043c5f545255f9ecef4a6)

## 0.3.1
2023-05-21

### Fixes

- further manifest optimisations (7262e613ef958a9d21e2361011d9044e18617928)

## 0.3.1-develop.1
2023-02-19

### Fixes

- further manifest optimisations (7262e613ef958a9d21e2361011d9044e18617928)

## 0.3.0
2022-09-27

### Features

- @jamiejshunter finalised local filesystem metadata repository work (a6d131089c30601bf0ea3f149fa0f1524d1240ce)
- @jamiejshunter updates for manifest (8b042c3e62691170782b1743b9ab63aafef1ef09)

### Fixes

- add build for M1 macs - develop branch only (780378044dd7a1f0dca29c7ef3b1e3269cd30482)
- update ionfs packages to net6.0 (bc20e17f6ade7f04cab6f90555f2b01cc4545d31)
- update dependencies for .net6.0 (ca240815760a10892e25f56c48521db20c7b6c92)
- add Microsoft.Extensions.Configuration.Binder for upgrade to net6.0 (3e8f19ba0bfcb4293b5e16591dc5c3e27e5a5eb0)
- minor package updates and @jamiejshunter updates to IonFS (b05273cbab9d8049641dbb3753f8fe18965389b9)
- minor fixes, initial LocalFS repo (eb66aa771de247f8a738c992a0eb31194675a241)
- @jamiejshunter bug fix for S3 Metadata Exists (35b959495ea4447bd38fcad1c830b646c41b0962)
- @jamiejshunter fixes for ListAsync (2fad6eb2f274bc81820db2a1d60a549b72e95744)
- upgrade Ionburst.SDK to 1.2.6 - fixes assembly version issue (1c5c19ba25eb49ccf19cc0843d0d59ae461b3b05)
- update System.CommandLine package (b4682296b3065a35bd7d47de159352f2c3fac3f1)
- remove unnecessary trailing slash from list output (5a260e0ad38193836ef990a7c9cd38de3a2da297)
- @jamiejshunter updates - repo/policy commands, system.commandline (bd6189c49f276db05a383092255b95504437027d)

## 0.3.0-develop.5
2022-08-09

### Features

- @jamiejshunter updates for manifest (8b042c3e62691170782b1743b9ab63aafef1ef09)

### Fixes

- @jamiejshunter updates - repo/policy commands, system.commandline (bd6189c49f276db05a383092255b95504437027d)

## 0.3.0-develop.4
2022-03-11

### Features

- @jamiejshunter finalised local filesystem metadata repository work (a6d131089c30601bf0ea3f149fa0f1524d1240ce)

### Fixes

- update dependencies for .net6.0 (ca240815760a10892e25f56c48521db20c7b6c92)
- minor package updates and @jamiejshunter updates to IonFS (b05273cbab9d8049641dbb3753f8fe18965389b9)
- minor fixes, initial LocalFS repo (eb66aa771de247f8a738c992a0eb31194675a241)
- @jamiejshunter bug fix for S3 Metadata Exists (35b959495ea4447bd38fcad1c830b646c41b0962)
- @jamiejshunter fixes for ListAsync (2fad6eb2f274bc81820db2a1d60a549b72e95744)
- update System.CommandLine package (b4682296b3065a35bd7d47de159352f2c3fac3f1)
- remove unnecessary trailing slash from list output (5a260e0ad38193836ef990a7c9cd38de3a2da297)

## 0.3.0-develop.3
2022-03-09

### Features

- @jamiejshunter finalised local filesystem metadata repository work (a6d131089c30601bf0ea3f149fa0f1524d1240ce)

### Fixes

- update dependencies for .net6.0 (ca240815760a10892e25f56c48521db20c7b6c92)
- minor package updates and @jamiejshunter updates to IonFS (b05273cbab9d8049641dbb3753f8fe18965389b9)
- minor fixes, initial LocalFS repo (eb66aa771de247f8a738c992a0eb31194675a241)
- @jamiejshunter bug fix for S3 Metadata Exists (35b959495ea4447bd38fcad1c830b646c41b0962)
- @jamiejshunter fixes for ListAsync (2fad6eb2f274bc81820db2a1d60a549b72e95744)
- update System.CommandLine package (b4682296b3065a35bd7d47de159352f2c3fac3f1)

## 0.3.0-develop.2
2022-03-07

### Features

- @jamiejshunter finalised local filesystem metadata repository work (a6d131089c30601bf0ea3f149fa0f1524d1240ce)

### Fixes

- update dependencies for .net6.0 (ca240815760a10892e25f56c48521db20c7b6c92)
- minor package updates and @jamiejshunter updates to IonFS (b05273cbab9d8049641dbb3753f8fe18965389b9)
- minor fixes, initial LocalFS repo (eb66aa771de247f8a738c992a0eb31194675a241)
- @jamiejshunter bug fix for S3 Metadata Exists (35b959495ea4447bd38fcad1c830b646c41b0962)
- @jamiejshunter fixes for ListAsync (2fad6eb2f274bc81820db2a1d60a549b72e95744)
- upgrade Ionburst.SDK to 1.2.6 - fixes assembly version issue (1c5c19ba25eb49ccf19cc0843d0d59ae461b3b05)

## 0.3.0-develop.1
2022-03-06

### Features

- @jamiejshunter finalised local filesystem metadata repository work (a6d131089c30601bf0ea3f149fa0f1524d1240ce)

### Fixes

- update dependencies for .net6.0 (ca240815760a10892e25f56c48521db20c7b6c92)
- minor package updates and @jamiejshunter updates to IonFS (b05273cbab9d8049641dbb3753f8fe18965389b9)
- minor fixes, initial LocalFS repo (eb66aa771de247f8a738c992a0eb31194675a241)
- @jamiejshunter bug fix for S3 Metadata Exists (35b959495ea4447bd38fcad1c830b646c41b0962)
- @jamiejshunter fixes for ListAsync (2fad6eb2f274bc81820db2a1d60a549b72e95744)

## 0.2.5-develop.4
2021-12-24

### Fixes

- update dependencies for .net6.0 (ca240815760a10892e25f56c48521db20c7b6c92)
- minor package updates and @jamiejshunter updates to IonFS (b05273cbab9d8049641dbb3753f8fe18965389b9)
- minor fixes, initial LocalFS repo (eb66aa771de247f8a738c992a0eb31194675a241)
- @jamiejshunter bug fix for S3 Metadata Exists (35b959495ea4447bd38fcad1c830b646c41b0962)
- @jamiejshunter fixes for ListAsync (2fad6eb2f274bc81820db2a1d60a549b72e95744)

## 0.2.5-develop.3
2021-12-10

### Fixes

- update dependencies for .net6.0 (ca240815760a10892e25f56c48521db20c7b6c92)
- add Microsoft.Extensions.Configuration.Binder for upgrade to net6.0 (3e8f19ba0bfcb4293b5e16591dc5c3e27e5a5eb0)

## 0.2.5-develop.2
2021-11-18

### Fixes

- update ionfs packages to net6.0 (bc20e17f6ade7f04cab6f90555f2b01cc4545d31)

## 0.2.5-develop.1
2021-07-28

### Fixes

- add build for M1 macs - develop branch only (780378044dd7a1f0dca29c7ef3b1e3269cd30482)

## 0.2.4
2021-07-28

### Fixes

- dependency updates for S3 and MongoDB (2a595af716b1704d219ea7b9c69b9d234bbbbc65)

## 0.2.4-develop.1
2021-07-28

### Fixes

- dependency updates for S3 and MongoDB (2a595af716b1704d219ea7b9c69b9d234bbbbc65)

## 0.2.3
2021-07-23

### Fixes

- minor updates to command language (b5633fd58a689c2dfd37bb81a77e27340e81ed20)
- minor formatting update for list (f6722970c493b01608bc24b9ffa90ccfeb303e3f)

## 0.2.3-develop.1
2021-07-23

### Fixes

- minor updates to command language (b5633fd58a689c2dfd37bb81a77e27340e81ed20)
- minor formatting update for list (f6722970c493b01608bc24b9ffa90ccfeb303e3f)

## 0.2.2
2021-07-21

### Fixes

- incorporate @jamiejshunter final fix [ci-skip] (590f6091ae86093d8958b214562da45ce067b20f)
- switch to .NET 5 (71ef48a84c5f3f829d4f3e7bf6e463144f45b2c1)

## 0.2.2-develop.1
2021-07-21

### Fixes

- incorporate @jamiejshunter final fix [ci-skip] (590f6091ae86093d8958b214562da45ce067b20f)
- switch to .NET 5 (71ef48a84c5f3f829d4f3e7bf6e463144f45b2c1)

## 0.2.1
2021-07-21

### Fixes

- swap method used to get ionfs version (02014db4c0ed403703a4e80f2da77f99cc70c90b)

## 0.2.1-develop.1
2021-07-20

### Fixes

- swap method used to get ionfs version (02014db4c0ed403703a4e80f2da77f99cc70c90b)

## 0.2.0
2021-07-16

### Features

- add secrets functionality to ionfs (courtesy of @jamiejshunter) (ba8f5aff22645244503f22e0c9d09eecff439a50)

### Fixes

- README updates (5dfc6504e97b68fa4452b006baf82ed79dfac416)

## 0.2.0-develop.1
2021-07-15

### Features

- add secrets functionality to ionfs (courtesy of @jamiejshunter) (ba8f5aff22645244503f22e0c9d09eecff439a50)

### Fixes

- README updates (5dfc6504e97b68fa4452b006baf82ed79dfac416)

## 0.1.2
2021-07-15

### Fixes

- Update .gitlab-ci.yml - release links were broken[ci-skip] (7363a125d6910de53f323de3222b7be14eb28c79)
- revert System.CommandLine (1fd6cff45b0698ed92c464ba357d8f664d556d99)

## 0.1.2-develop.1
2021-07-15

### Fixes

- Update .gitlab-ci.yml - release links were broken[ci-skip] (7363a125d6910de53f323de3222b7be14eb28c79)
- revert System.CommandLine (1fd6cff45b0698ed92c464ba357d8f664d556d99)

## 0.1.1
2021-04-06

### Fixes

- dependency updates for ionfs (7aeaf35916404f255377547f01eedf2c454170b5)
- revamp ci to automate versioning and releases (74d3d85ee3d8b039661424995de06f0adc43d21a)
- add release details to ci (c0d0b08815d50a1662b2bb6cf0bcc7d806f7a5f4)
- path fixes for ci (f9bc206ef347d2626a0d9476686f78c5be3a95d5)
- final fix for windows ci (15101501465a01efc903e99c805a899d91f7d87e)
- final publish fix to attach urls to download properly (0be249f60e8ab39d1b0ce6c002eab38e4fc3cb29)

## 0.1.1
2021-04-06

### Fixes

- dependency updates for ionfs (7aeaf35916404f255377547f01eedf2c454170b5)
- revamp ci to automate versioning and releases (74d3d85ee3d8b039661424995de06f0adc43d21a)
- add release details to ci (c0d0b08815d50a1662b2bb6cf0bcc7d806f7a5f4)
- path fixes for ci (f9bc206ef347d2626a0d9476686f78c5be3a95d5)
- final fix for windows ci (15101501465a01efc903e99c805a899d91f7d87e)