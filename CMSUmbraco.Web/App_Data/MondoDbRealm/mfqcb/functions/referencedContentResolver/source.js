exports = async (source) => {
    const { referencedContent } = source;

    if (referencedContent && referencedContent.length > 0) {
        const cluster = context.services.get("mongodb-atlas");
        const contentColl = cluster.db("PRXXXX").collection("Content");

        const result = [];
        for (var i = 0; i < referencedContent.length; i++) {
            const page = await contentColl.findOne({ "_id": referencedContent[i] });
            if (page) {
                result.push(page);
            }
        }

        return result;
    }

    return null;

};