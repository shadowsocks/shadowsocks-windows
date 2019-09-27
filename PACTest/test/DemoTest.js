const abp = require("../abpwrapper")

test("Demo test, is it working", () => {
    abp.rules = [".google.fr"]
    abp.userrules = []
    const ret = abp.FindProxyForURL("http://www.google.fr/", "www.google.fr")
    expect(ret).toBe("__PROXY__")
})
