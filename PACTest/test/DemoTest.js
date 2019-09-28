const abp = require("../abpwrapper");

describe("Demo test", () => {
    it("Is it working?", () => {
        abp.rules = [".google.fr"];
        abp.userrules = [];
        const ret = abp.FindProxyForURL(
            "http://www.google.fr/",
            "www.google.fr"
        );
        expect(ret).toBe(abp.proxy);
    });
});
