// Just a placeholder, add test in test/ directory
const abp = require("./abpwrapper");
abp.rules = [".google.fr"];
abp.userrules = [];
const ret = abp.FindProxyForURL("http://www.google.fr/");
console.log(ret);
