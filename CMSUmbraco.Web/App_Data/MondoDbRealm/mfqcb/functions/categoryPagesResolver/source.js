exports = async (source) => {
    const { categories } = source;

    if (categories && categories.length > 0) {
        const cluster = context.services.get("mongodb-atlas");
        const contentColl = cluster.db("PRXXXX").collection("Content");

        const result = [];
        for (var i = 0; i < categories.length; i++) {
            const page = await contentColl.findOne({ "_id": categories[i] });
            if (page) {
                result.push(page);
            }
        }

        return result;
    }

    return null;

};
